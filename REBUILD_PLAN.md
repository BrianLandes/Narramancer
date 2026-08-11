# Narramancer Rebuild Plan — Graph Toolkit + Decoupled Core + Custom Serializer

> Status: DRAFT for review. Produced from a full codebase exploration of the current
> xNode-based implementation. No code has been changed yet.

## Context — why this rebuild

The current framework is built on **xNode**, which (a) is limited to a single editor
window at a time, and (b) has required hacks to extend. Narramancer's real value is its
**runtime model** (Nouns / Verbs / Adjectives / Blackboards) and its **resumable save
system** — not the graph editor. xNode entangles the graph *data model*, the *editor
window*, and the *runtime* into one ScriptableObject-based thing, so every limitation
forces a workaround.

This plan rebuilds Narramancer around three goals:

1. Replace xNode with Unity's official **Graph Toolkit (GTK)** — multi-window, actively
   maintained, the framework Unity itself is committed to. GTK is *authoring-only* (no
   runtime execution backend), which is ideal because Narramancer already owns its runtime.
2. **Decouple the runtime/data model from Unity** so the bulk of logic is plain C#,
   unit-testable under .NET without the Unity Editor.
3. Reimplement the small subset of **Odin Serializer** actually used, and drop the dependency.

Plus three specific requirements from the maintainer:
- Verb runner resume position serialized as **(graph id + node id)** and rehydrated from there.
- **Play-mode editor feedback**: highlight which node is running / recently ran.
- Design must stay **friendly to cherry-picking** improvements from a separate, larger
  Narramancer project later.

### Decisions assumed in this draft (override to change scope)
- **Migration scope: re-author content + break old saves.** No xNode→Core graph importer,
  no save-file migrator. Example verbs (and your larger project's verbs) get rebuilt in the
  new GTK editor. (Alternatives: auto-migrate graphs only; or preserve graphs *and* saves —
  each adds substantial work in Phases 4–6.)
- **Unity target: require Unity 6.2+.** This is the new minimum supported version (fully
  Asset Store-compliant; it narrows the audience to upgraded users).

---

## 1. Target architecture — three assemblies

```
Narramancer.Core        (asmdef, noEngineReferences = true; mirror .csproj for CI)
  Domain/        Noun, NounId, AdjectiveInstance + Property/Stat/Relationship, Blackboard, StoryState
  Graph/         Graph, Node, Port, Connection, PortId, NodeId, GraphId (POCOs, stable string ids)
  Execution/     NodeRunner, Promise, RunnerPosition, RunContext, IClock, ILog
  Serialization/ NjSerializer, TypeRegistry, reference table, converters
  References/     IReferenceResolver, AssetHandle<T>, NounRef/VerbRef/PropertyRef/StatRef/RelationshipRef
  Nodes/         RunnableNode, ChainedRunnableNode, ValueNode, + the 156 ported node types
  Attributes/    [Node], [Input], [Output], [DynamicPorts], [NodeMenu], [NodeTint], [NodeWidth]

Narramancer.Unity       (asmdef, references Core)
  NarramancerSingleton (MonoBehaviour), SaveMenu/LoadMenu, SerializableMonoBehaviour
  UnityReferenceResolver : IReferenceResolver  (GUID <-> UnityEngine.Object)
  ScriptableObject template wrappers (Noun/Property/Stat/Relationship)
  VerbGraphAsset (ScriptableObject wrapping a Core Graph + GTK presentation sidecar)
  Save file IO, screenshots, scene management, Time.time/timer bridge

Narramancer.Editor      (asmdef, Editor-only, references Core + Unity + GTK)
  GTK graph window(s) — multi-window, node-view mapping, search, drag/drop, collapse-to-subverb
  Play-mode visual feedback (observes runner state by polling)
```

**Hard rule:** `Narramancer.Core` must compile and test in a plain `dotnet test` project
that does **not** reference UnityEngine. Enforced by asmdef `noEngineReferences` + a mirror
`.csproj` used in CI.

### Graph ownership: Core owns the canonical graph; GTK is a view
A `VerbGraphAsset` (Unity layer) stores a Core `Graph` POCO (nodes/ports/connections/ids)
plus a sidecar of GTK-only presentation (positions, groups, notes). The GTK window is a
view/controller: opening it builds GTK views from the POCO; edits mutate the POCO + sidecar.

Why not "GTK owns the asset, compile to a runtime graph":
1. **Resume-by-id needs stable node ids surviving round-trips.** A recompile risks
   reassigning ids and breaking saved runner positions. Core-owned ids are minted once.
2. **GTK is experimental** — Core-owns makes GTK a *replaceable* view.
3. **Cherry-pick goal** — node/execution work lives in Core, decoupled from the editor.
4. **No runtime compile step** — the loaded asset is already executable.

Trade-off accepted: a bidirectional GTK↔POCO sync layer (bounded editor code; the natural
place to absorb GTK API churn).

### Getting Core off UnityEngine (grounded substitutions)
- `NodeRunner` uses `Time.time` and `Debug.Log` → injected `IClock` (`float Now`) + `ILog`
  on a `RunContext`. Tests supply a fake clock. (`NodeRunner.cs`)
- Drop `[SerializeField]` in Core; the custom serializer decides inclusion via its own
  attribute (`[NjSerialize]` / `[NjIgnore]`). `[NonSerialized]` (System) stays.
- `Blackboard` GameObject/Component/UnityObject dictionaries → handles; the already
  `[NonSerialized]` GameObject/Component slots stay **live-only** (not persisted). (`Blackboard.cs`)
- `NounInstance.GameObject` has no `[SerializeField]` — already runtime-only; keep as a
  live-only slot resolved on demand. (`NounInstance.cs`)

### The reference keystone
```csharp
readonly struct AssetHandle<T> { string Id; }   // Id = GUID string
interface IReferenceResolver {
    bool TryResolve(string id, Type type, out object asset);
    bool TryGetId(object asset, out string id);
}
```
Verb graphs, noun/property/stat/relationship templates, and ingredients become **handles by
id**. The polymorphic ingredient lists live on the template assets, which Core only touches
through the resolver — so **Core never serializes ingredient polymorphism**, removing the
single hardest Odin feature. Unity's `UnityReferenceResolver` maps GUID↔`UnityEngine.Object`
via `AssetDatabase` (editor) and a **baked manifest** ScriptableObject (builds).

---

## 2. Node model — one class for execution *and* authoring

One C# class per node type that (a) executes at runtime in Core and (b) is reflected by GTK
to build authoring views — replacing xNode's `[Input]/[Output]`, `UpdatePorts()`, dynamic
ports, and `GetValue(context, port)`.

```csharp
abstract class Node {
    string Id;                       // stable GUID, minted once, serialized
    string DisplayName;
    IReadOnlyList<Port> Ports;       // from attributes + dynamic additions
    abstract object GetValue(RunContext ctx, Port port);   // pull-based
    virtual void OnPortsChanged() {} // replaces xNode UpdatePorts()
}
class Port       { string Id; string Name; PortDirection Dir; Type ValueType; ConnectionType ConnType; bool IsDynamic; }
class Connection { PortRef From; PortRef To; }   // PortRef = (NodeId, PortId)
class Graph      { GraphId Id; List<Node> Nodes; List<Connection> Connections; List<InputPort> Inputs; List<OutputPort> Outputs; }
```

Key change vs xNode: **connections are stored on the Graph keyed by (NodeId, PortId)**, not
as object refs inside `NodePort`. This makes the graph a pure serializable POCO and is the
foundation of resume-by-id.

Attribute-driven ports are read by a shared `NodeReflectionCache` (Core) consumed by **both**
runtime and GTK, so authoring and execution can never disagree on ports/types.

Mapping the existing hierarchy:
- `RunnableNode`: keep `abstract void Run(NodeRunner)`, `Cancel`, the `thisNode` self-port.
  `TimeSinceLastRun` moves to the injected clock. (`RunnableNode.cs`)
- `ChainedRunnableNode`: keep the `thenRunNode` output. The "next node" is resolved by
  following the connection to a `NodeId`, then `graph.GetNode(nodeId)` — instead of
  `port.GetConnections()[0].node`. **This is the crucial change enabling id-based resume.**
- `AbstractDynamicMethod{Runnable,Value}Node`: reflection-driven dynamic ports map directly;
  `RebuildPorts()` → `OnPortsChanged()`; `GetOrAddDynamicInput/Output` + `ClearDynamicPortsExcept`
  become Core `Node` methods. Each dynamic port gets a **stable GUID id at creation** so
  connections survive rebuilds.
- Value flow (`GetValue`/`GetInputValue`, ~355 sites) stays pull-based via
  `Node.GetInputValue<T>(ctx, portName)`. `INodeContext` (today an empty marker) becomes
  `RunContext` carrying `Blackboard`, `IReferenceResolver`, `IClock`, `ILog`, and the owning `Graph`.

Dropping ScriptableObject removes the `Node.graphHotfix` workaround and makes nodes trivially
constructible in tests (`new FooNode()`).

---

## 3. Serializer replacement — `Narramancer.Core.Serialization.NjSerializer`

Odin is used at exactly **2 sites** (`SaveLoadUtilities.cs`): `SerializeValue<T>(data, JSON,
out objects)` / `DeserializeValue<T>(bytes, JSON, objects)`. JSON only. Needed:
1. Reflection field serialization of POCO/`[Serializable]` types.
2. Recursive object graphs; `List<T>`, arrays, nested types, `Dictionary<,>`.
3. Polymorphic type tags (abstract base → concrete) — node lists, adjective-instance lists.
   Smaller now that asset/ingredient polymorphism is behind handles.
4. External reference table for asset handles (id strings) — replaces Odin's
   `List<UnityEngine.Object> objects`.
5. Cycles / shared refs.

NOT needed: binary/nodes formats, custom formatters, `ISerializable`, `[SerializeReference]`.

Design:
- Serialize to an intermediate `JsonValue` tree, then emit text. Use `System.Text.Json` as
  the tokenizer if available on the 6.2 runtime; otherwise ship a ~200-LoC hand-written
  tokenizer in Core. Object-graph logic is ours either way (fully testable).
- Polymorphic values emit `"$type":"<short-name>"` via a `TypeRegistry` (short stable names ↔
  Type), populated by scanning `[Node]`/`[NjPolymorphic]` types. Short names keep saves portable.
- Asset handles serialize as **id strings only**; resolution is deferred to `IReferenceResolver`
  in the Unity layer. Object identity/cycles via `"$id"`/`"$ref"`.
- Envelope: `{ schemaVersion, typeRegistryVersion, payload, references:[ids] }`. **Ship
  `schemaVersion` from day one.** The Unity layer wraps with title/thumbnail/screenshot and
  writes the file (replaces `SaveDataWrapper`).

### Callback serialization (do not overlook)
`Promise` (the object a suspended ActionVerb awaits) stores `doneCallbacks` as
`SerializableAction` (the SerializableActionHelper plugin). This is how a suspended verb's
*continuation* survives save/load — central to resume. The Core port must absorb this
delegate/callback serialization. Audit `SerializableAction` for Unity coupling (it likely
references method targets that may be `UnityEngine.Object`s) and move the core to
`Narramancer.Core` with a Unity bridge for any object-target resolution.

---

## 4. Resume-by-id

```csharp
struct RunnerPosition { string GraphId; string NodeId; }
class NodeRunner {
    RunStatus status;
    RunnerPosition? running;        // was: RunnableNode runningNode
    List<RunnerPosition> queued;    // was: List<RunnableNode> queuedNodes
    List<NodeRunnerEvent> recent;   // { GraphId, NodeId, name, timeStamp }
    Blackboard blackboard;
    Promise promise;
}
```
- Positions serialize as (GraphId, NodeId) strings — no object-graph dependency.
- Rehydration: `Update()` resolves the running position lazily via
  `resolver.ResolveGraph(GraphId).GetNode(NodeId)` (cache after first resolve), then `Run(this)`.
- `Graph.GetNode(nodeId)` is O(1) via a dict built on load.
- **Node id stability is the contract:** ids minted once at authoring, never re-minted by
  GTK round-trips; GraphId = asset GUID. Add an editor validation pass asserting node-id
  uniqueness and no reassignment.
- Keep the existing flag-based, non-coroutine suspend/resume model (`Suspend()`/`Resume()`,
  `postCurrentNodeBehavior`, `doPostRunLogic`). Keep promises id-addressable so a resumed
  timer re-binds the right promise.

---

## 5. Play-mode visual feedback

The data already exists: `NodeRunner.recentlyRunNodes` + the current running position +
`NodeRunnerEvent.timeStamp`/`name` ("Ran"/"Canceled"). After the rebuild these are
(GraphId, NodeId)-keyed, matchable to GTK node views by id.

One-way coupling (editor pulls; runtime never depends on editor):
- Core defines `IRunnerObserver`; builds use a no-op.
- Unity exposes active runners (already enumerable via `storyInstance.NodeRunners`) and a
  "recent events for GraphId X" query.
- The editor, in `EditorApplication.update` during play mode, **polls** the singleton for the
  open graph's runners and reads `running`/`recent`.

Rendering: match each GTK node view's `NodeId` → running = "running" style; recent within a
fade window (`Now - timeStamp`) = decaying highlight; "Canceled" = distinct tint.
**Multi-window GTK lets us highlight across several open verb graphs at once** — a concrete
win over xNode.

---

## 6. Test strategy

### Bucket A — Core unit tests (`dotnet test` / NUnit, no Unity) — runs in CI, highest value
1. Serializer round-trips: primitives, collections, dicts, nested POCOs, polymorphic node
   lists, cycles, handle-id preservation, unknown-`$type` handling, schema-version gate.
2. `NodeRunner` suspend/resume with a fake `IClock`.
3. Resume-by-id across a serialize boundary (flagship).
4. Domain ops: NounInstance properties/stats/relationships, bidirectional relationship
   integrity by NounUID, Blackboard typed set/get/remove, StoryState create/clear.
5. Graph model: node-id uniqueness, connection resolution by (NodeId, PortId), dynamic-port
   id stability, attribute-derived port sets.
6. Reference resolver contract via a fake resolver — assert Core needs only ids.

### Bucket B — Unity EditMode (local only)
UnityReferenceResolver GUID↔asset mapping; GTK↔POCO sync (edit, write back, ids preserved);
VerbGraphAsset serialization; Save/Load file IO end-to-end.

### Bucket C — Unity PlayMode (local only)
Full verb play-through; suspend via timer; save; reload; resume. Play-mode highlight matches
running/recent. Parallel / instance-input / dynamic-method nodes against real Unity APIs.

### First tests to write (in order)
1. Serializer round-trip of a Blackboard + small POCO graph.
2. `NodeRunner` runs a 3-node chain to completion with a fake clock.
3. Suspend/resume across a serialize boundary (resume-by-id flagship).
4. Relationship bidirectional integrity by UID.

### CI
`Narramancer.Core.Tests` (.NET 8) references the Core `.csproj` mirror and runs on every
push. Unity EditMode/PlayMode run locally (documented runbook); optionally a self-hosted 6.2
runner later. NOTE: this environment can run Bucket A only (no Unity); a SessionStart hook
can auto-install the .NET SDK so web sessions run Core tests automatically.

---

## 7. Phasing (old xNode + new Core coexist until Phase 6)

**Phase 0 — Scaffolding & CI.** Core asmdef (`noEngineReferences`) + mirror `.csproj` +
`Narramancer.Core.Tests` in CI. Exit: green CI on a trivial test; Unity still builds with
xNode untouched.

**Phase 1 — Engine upgrade to 6.2+ (xNode still in place).** De-risk the upgrade
independently of GTK. Exit: parity on 6.2 with the old framework.

**Phase 2 — Core skeleton.** Domain + serializer + graph/runner POCOs (no nodes ported yet).
Exit: serializer round-trips Blackboard + StoryState; runner executes an in-code chain; 40+
Core tests green in CI.

**Phase 3 — Vertical slice (GTK go/no-go gate).** Port ~6 representative nodes (RootNode, a
ChainedRunnableNode like PrintText, a ValueNode, a ConditionalNode, a suspend node like
Wait/StartTimer, one dynamic-method node). Build minimal Narramancer.Unity (VerbGraphAsset,
UnityReferenceResolver, clock/timer bridge) and ONE GTK window with create/connect/delete +
save. Exit: author a trivial verb in GTK, run in play mode, see the highlight, save
mid-suspend, reload, resume to completion.

**Phase 4 — Bulk node port (the 156).** Waves by family: literals/value → list ops → domain
(noun/property/stat/relationship) → control flow/parallel → dynamic-method/reflection →
Unity-API nodes. Per wave: port to Core attributes, unit-test engine-free logic, register
`$type` names. This is the natural point to compare against the larger project and
**cherry-pick its improvements** (Core nodes are engine-free and merge-friendly by design).

**Phase 5 — Editor parity.** GTK matches the current `VerbGraphEditor`: nested-graph
navigation, search popup, drag-drop, collapse-to-subverb, port styling, multi-window
play-mode debugging (replaces the 30+ `[CustomNodeEditor]` classes).

**Phase 6 — Cutover & removal.** Delete `Scripts/Plugins/xNode`; replace both Odin call
sites with `NjSerializer`; remove dead `SerializableDictionary`/`SerializableType` hacks.
Exit: no xNode/Odin references; all buckets green; package builds for distribution.

---

## 8. Risks & open items
- **GTK experimental / API churn (HIGH)** — mitigated by Core-owns-graph (GTK is a
  replaceable view), the Phase 3 gate, and a thin `IGraphView` adapter. Verify in Phase 3
  that the target GTK version supports custom port colors/types, runtime-added dynamic ports,
  and per-node decorations for highlighting.
- **Engine upgrade 2021.3 → 6.2+ (MEDIUM)** — isolated in Phase 1. Watch UI Toolkit,
  screenshot/thumbnail, input API breakages.
- **Save migration (assumed: break)** — old saves are Odin/object-ref based; new are id-based.
  Ship `schemaVersion` from Phase 2; show a clear "incompatible save" message, don't crash.
- **Old+new coexistence (MEDIUM)** — distinct namespaces (`Narramancer.Core.*`) and
  assemblies to avoid `Node`/`Blackboard`/`NodeRunner` name collisions.
- **Reference resolution in builds** — recommend a baked manifest ScriptableObject for v1
  (Addressables later if needed).
- **`System.Text.Json` on the 6.2 runtime (LOW)** — confirm; otherwise ship the small
  hand-written tokenizer.
- **`SerializableAction`/`Promise` callback serialization** — audit for Unity coupling; port
  core to Core with a Unity bridge (see §3).

## Key current files to port/replace
- `Assets/Narramancer/Scripts/Data/NodeRunner.cs`
- `Assets/Narramancer/Scripts/Nodes/RunnableNode.cs` (+ `ChainedRunnableNode.cs`, `AbstractDynamicMethodRunnableNode.cs`)
- `Assets/Narramancer/Scripts/Data/Promise.cs` (+ SerializableActionHelper plugin)
- `Assets/Narramancer/Scripts/Utilities/SaveLoadUtilities.cs`
- `Assets/Narramancer/Scripts/Data/Blackboard.cs` (+ `StoryInstance.cs`, `NounInstance.cs`)
- `Assets/Narramancer/Scripts/Editor/VerbGraphEditor.cs`
