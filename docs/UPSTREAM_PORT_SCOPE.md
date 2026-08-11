# Upstream Port Scope — Pseudo World Gaia → Narramancer

Scoping doc for carrying two years of in-the-field changes back from the working copy of the plugin
living in the *Pseudo World Gaia* project into this repo.

- **Source:** `/home/brian/Exodus_2026/Unity Projects/Pseudo World Gaia/Assets/Plugins/Narramancer`
- **Target:** `Assets/Narramancer` (this repo)
- **Surveyed:** 2026-08-11

---

## The delta, measured

| | Count |
|---|---|
| `.cs` files in common | 566 |
| …of which genuinely differ (ignoring CRLF/whitespace) | **54** |
| Files only in Pseudo World Gaia (new work) | **32** |
| Files only here (this repo is ahead) | 4 |

The OdinSerializer and xNode vendored trees are **byte-identical modulo line endings** — apart from three
files. That's the good news: the fork never drifted into the dependencies, so nothing here collides with
the planned Odin removal.

Outside the plugin folder, ~60 files under `Assets/Scripts/` reference Narramancer (combat, inventory,
build system, confrontation, EQS). All game-specific — nothing to port, but useful as evidence of what a
real consumer of the plugin reaches for (see [Design signals](#design-signals-not-code) below).

### Direction check — this repo is newer in places

The fork is frozen around **Nov 2025**; this repo has 2026 commits. Several "diffs" are this repo being
ahead, and must **not** be reverse-ported:

| File | Why it looks different |
|---|---|
| `Components/SaveMenu.cs`, `Components/LoadMenu.cs` | `Awake()`→`Start()` and coroutine→`async void` are the *old* code. The versions here carry explanatory comments about `OnEnable()` ordering and the end-of-frame buffer read. |
| `Utilities/SaveLoadUtilities.cs` | Fork lacks the `SaveFileSync.Flush()` WebGL fix. |
| `Data/Singleton*.cs`, `Utilities/SaveFileSync.cs` | Exist only here. |
| `Data/CategoryPropertyModifierIngredient.cs` | Fork blanked the doc comment. |

---

## Tier A — Bug fixes. Small, self-contained, port now

Every one of these is a real defect that still exists here. None need the test net; all are 1–10 line
changes. Good warm-up work alongside `tasks.md` §1.

| # | Where | The bug |
|---|---|---|
| A1 | `Editor/VerbGraphInspector.cs:26,35` | **The `#if ODIN_INSPECTOR` branch does not compile.** Calls `DuplciateNodeGraphField` (typo'd; the method is `DuplicateNodeGraphField`) and casts `target as NarramancerGraph`, a type that doesn't exist in this repo. Dead code here, but it's the *only* branch an Odin Inspector owner compiles — i.e. exactly the buyer segment `tasks.md` §2 is trying not to lose. Highest value-per-line in the whole survey. |
| A2 | `Data/StatInstance.cs:16-24` | Min/max clamp reads the **parameter** `value`, not the already-clamped `this.value`, so the first clamp is silently discarded. A stat with both a min and a max only honors the max. |
| A3 | `Nodes/AddRelationshipNode.cs:28` | `if (instance != null \|\| relationship != null)` — should be `&&`. Null-derefs whenever exactly one is set. |
| A4 | `Data/NounInstancesQuery.cs` | `Equals`/`GetHashCode` null-deref when either property array is unset. The fork adds guards. ⚠️ **The fork's fix has its own typo** — one branch tests `mustHaveProperties.ContainsAll(otherQuery.mustNotHaveProperties)`. Fix it on the way in. |
| A5 | `Nodes/GameObjectDistanceCheckNode.cs:41` | No null check on either GameObject before reading `.transform`. |
| A6 | `Editor/VerbGraphEditor.cs` `GetPortStyle` | No guard for `port == null \|\| port.ValueType == null`; throws while a dynamic port is mid-rebuild. |
| A7 | `Editor/NarramancerGraphEditorUtilities.cs:83,109` | Two `while` loops that hang the editor on a cyclic node graph. Fork adds `&& !bucket.Contains(node)`. |
| A8 | `Editor/NamedPrimitiveValueListDrawer.cs`, `Editor/NounScriptableObjectListDrawer.cs` | Both cache `Resources.Load<Texture2D>("d_winbtn_win_close@2x")`, which resolves to `null` — the search-clear button renders blank. Fork swaps to `EditorGUIUtility.IconContent("winbtn_win_close")`. |
| A9 | `Utilities/AssemblyUtilities.cs:274` | The fuzzy-AQN regex only matches a **single-segment** namespace, so any type in `A.B.C` fails the fallback. ⚠️ `tasks.md` §1 plans to *delete* lines 285–299 outright — if that lands first, skip A9. |
| A10 | `Nodes/AbstractDynamicMethodValueNode.cs:148` | Silent null target object; fork logs it. Cosmetic but cheap. |

---

## Tier B — Features worth carrying. Real design value

### B1. Sub-runner tree + node cancellation ⚠️ *save-format change*

The single largest and most valuable item. `NodeRunner` gains a serialized `List<NodeRunner> subrunners`;
`StopAndReset()` recurses into them and releases each. Every node that spawns a sub-runner
(`ChoosePrioritizedBranchNode`, `RunActionVerbWhileConditionIsTrueNode`) registers and deregisters it,
and `RunnableNode.Cancel(NodeRunner)` — already declared virtual here and already invoked from
`NodeRunner.cs:154`, but overridden by nothing — finally gets real implementations
(`WaitNode` cancels its timer, `RunActionVerbWhileConditionIsTrueNode` tears down its sub-runner).

Today, stopping a runner mid-flight **leaks** every sub-runner it started: they keep ticking against a
blackboard nobody owns. That's a correctness bug in the execution model, not a nicety.

Also in this cluster: `NodeRunner.IsRunningOrSuspended()`, `NodeRunner.ClearQueuedNodes()`,
`NarramancerSingleton.RemoveTimer(Promise)`, and `RunnableNode.TimeSinceLastRun` walking the sub-runner
tree.

**Sequencing:** `subrunners` is a serialized field, so this alters the save format. Land it during
`tasks.md` §2 while breaking the format is still free — not after.

### B2. Promise registry on the singleton ⚠️ *completes dead code*

`StoryInstance` already has `StringPromiseDictionary promises` with a public accessor — and **nothing in
this repo reads or writes it.** The fork finishes the job: `NarramancerSingleton.MakePromise()` /
`BreakPromise()` / `UpdatePromises()` driven from `Update()`, plus `Promise.WithUpdate(Action)`,
`Promise.removeOnResolve`, and a `Promise.DefaultDone` singleton.

Worth doing for the dead-code cleanup alone. Same sequencing note as B1 — it puts live data into a
serialized dictionary that currently ships empty.

### B3. `InequalityNode` — the comparison operators are wrong *(free migration)*

Today `Comparison.GreaterThan` evaluates `>=` and `LessThan` evaluates `<=`. The fork adds the two
missing operators and fixes the semantics. **The enum reordering happens to be migration-safe:** old
index 1 (`GreaterThan`, behaving as `>=`) maps to new index 1 (`GreaterThanOrEqualTo`, `>=`), and old
index 2 likewise. Existing authored graphs keep their exact behavior and gain correct labels. Rare
freebie — take it.

### B4. `EditorDrawerUtilities` — nested `SerializedProperty` resolution

`GetTargetObject`/`GetFieldInfo` only handle top-level fields today, so any drawer used on a field
*inside* a serializable class silently fails. The fork walks dotted property paths and adds
`GetParentType()`, `GetPropertyTargetType()`, `GetTargetObjectParent()`, `GetFirstFieldWithType<T>()`.
`VerbGraphDrawer` is then fixed to use them (attribute lookup was resolving against the wrong type).

Infrastructure — B7 and several Tier-C items depend on it.

### B5. `VariableAssignment` — partial-match recovery

Renaming a verb input currently **wipes** every assignment bound to it, silently, across every graph and
component that referenced it. The fork matches on *either* id or name (with a type match) and repairs the
stale half. Pure authoring-quality fix; nothing about it is game-specific.

Ships alongside a new `VariableAssignmentList` type + `VariableAssignmentListDrawer` (reorderable list,
"Update Assignments"/"Clear Assignments" buttons) that wraps the same logic for reuse. Optional; needs B4.

### B6. Graph editor navigation

Fills two `// TODO: include NarramancerSingleton` comments in `VerbGraphEditor.cs`:
- **History dropdown** next to Back, listing the recently-opened stack (with null-graph skipping and a
  "Clear history" item), and a corrected Back stack — the current one mutates the stack in `OnOpen()` and
  loses position.
- **Live runner inspection of the singleton** — the runner picker now offers `NarramancerSingleton` and
  enumerates `StoryInstance.NodeRunners` during play. Directly useful for the demo video in §3.
- A `mostOpenedGraphs` frequency counter on the singleton (groundwork, unused).
- Skip the `NARRAMANCER_SHOW_FPS` block — debug scaffolding.

### B7. `ChooseRankedWeightedActionNode` doesn't forward graph inputs

Missing both `UpdatePorts()` (so the node never grows ports for its actions' verb inputs) and
`AssignGraphVariableInputs()` (so the chosen effect graph runs with unassigned inputs). The node is
effectively only usable with zero-input action verbs today. Fork adds both, matching the pattern already
used by `OfferObjectsAsChoicesNode` / `ListFilterNode`.

### B8. Noun-instance initialization order

`CreateNounForGameObject` gains `[DefaultExecutionOrder(-100)]`, an idempotence guard on `Start()`
(`Instance == null || Instance.GameObject != gameObject`), an optional `randomizeUid`, and a public
`Start()`; `SerializeNounInstanceReference.GetInstance()` lazily forces creation instead of returning
null when queried before `Start()`.

Fixes a real race — anything that asks for a noun instance during another component's `Awake`/`Start`
gets null today. ⚠️ Overlaps the `Saveable` refactor's GUID identity work
([`SAVEABLE_REFACTOR_HANDOFF.md`](SAVEABLE_REFACTOR_HANDOFF.md)); read that first and fold this in rather
than porting it standalone.

### B9. Choice authoring ergonomics

`OfferChoicesNode.AddChoiceNode(AddChoiceOption)` — add at top *or* bottom, surfaced as a menu in
`OfferChoicesNodeEditor`. Plus `ChoiceNode.HookUpToOfferChoicesNode()`, a button that cycles a choice
through the graph's offer nodes. The latter is `[Button]`-attributed (see Tier D on Odin).

### B10. Trivia worth taking with anything above

`Blackboard.UniqueKey(string, params object[])` · `NounInstance.HasProperties(IEnumerable<>)` ·
`NounInstance.AddRelationship` returning the instance (and reusing an existing one instead of dropping
the call) · `NounInstance.IsValid()` extension · `NarramancerSingleton.GetInstance(GameObject)` /
`GetInstance(PropertyScriptableObject)` / `GetInstancesWithProperty()` ·
`ListExtensions.TryChooseOne<T>` · `SerializableVariableReference.Scope` gaining a setter (plus the
drawer auto-falling back from Scene to Verb scope when the scene has no variables) ·
`Instancable` moved out of `CreateInstanceNode` into `IInstancable.cs` where it belongs ·
`SequenceOfNodesNode` `[NodeTint]`.

---

## Tier C — New nodes and components. Cheap adds, judgment call

**Generic, no dependencies, port freely:**

| File | Notes |
|---|---|
| `MinFloatNode`, `MaxFloatNode` | Textbook. |
| `GetDistanceBetweenGameObjectsNode` | With a `squared` toggle. |
| `RemoveBlackboardVariableNode`, `RemoveInstanceBlackboardVariableNode` | `Blackboard.Remove(key, Type)` already exists here — the nodes were just never written. Fills an obvious gap next to the existing Set/Get nodes. |
| `ClearRunningNodesNode` | 10 lines; needs `NodeRunner.ClearQueuedNodes()` from B1. |
| `FindGameObjectWithTypeNode` | Uses the deprecated `FindObjectOfType`; route through `GameObjectExtensions` per the repo convention. |
| `FirstTimeConditionalNode` | "Run this branch only the first time" — genuinely useful in narrative graphs. Currently in the global namespace; move into `Narramancer`. |

**3D / AI-flavored — port only if v1 is meant to serve 3D games:**

| File | Notes |
|---|---|
| `SerializeNavMeshAgent` | **Worth reading even if you don't port it.** It's the best real-world example of a hand-written save driver in either tree — serializes agent enabled/stoppingDistance plus either a destination `Vector3` or a `Transform` full-path, and resumes a `Promise` on arrival. Exactly the shape the `IComponentSaver` registry needs as its reference driver. Recommend porting it as a **sample/driver**, not core. |
| `GetTransformSpeedNode` + `TrackColliderVelocity` | Velocity for non-rigidbody objects; the node auto-adds the tracker component. |
| `AreaMonoBehaviour` + `GetRandomNavPointInAreaNode` | ⚠️ Half-finished — only the `Circle` case is implemented; `Square`/`Rectangle` are empty `break`s. Finish or drop. |
| `SetImageFillToStat` | Sibling of the existing `SetSliderFromStat`. Straightforward. |
| `ChangeStatOverTime` | Trivial, but global namespace and no null guard on `GetInstance()`. |
| `PrefabNounIngredient` | Noun→prefab link with a `TryGetPrefab` extension. Small, general, useful. |

---

## Tier D — Do not port

**Paid Odin Inspector dependency.** A dozen fork files use `Sirenix.OdinInspector` (`[Button]`,
`[ShowIf]`, `[ReadOnly]`, `[HorizontalGroup]`, `[LabelText]`) and `Sirenix.Utilities.Editor`. This repo
ships the *free* OdinSerializer only and must stay compilable without Odin Inspector — that's the whole
premise of the §2 removal task. **Strip these attributes on the way in** for anything ported from Tier B/C;
`[Button]` in particular usually maps to a button in the node's existing `[CustomNodeEditor]`.

**His own frameworks.** `SingletonSystem` (a different singleton than this repo's `Narramancer.Singleton`),
`RevelRousers` / `RevelPrefabUtilsAsync` / `RevelRousersUtils`.

| Item | Why not |
|---|---|
| Event system (`EventSystemManager`, `EventListenerComponent`, `FireEventNode`, `IEventListener`) | Global namespace, built on `SingletonSystem`, and `OnEventFiredNode` — the piece that would actually connect it to graphs — is **100% commented out**, as is the matching `SubRun` plumbing in `RunActionVerbMonoBehaviour`. An abandoned experiment. The *idea* is good (see below); the code isn't the way in. |
| `SerializableSpawner` fork changes | Terrain sampling, ground raycasting, spawn-on-start counts, editor gizmos, `[Button]`s, `RevelRousers` calls. Game-specific. The one general bit — reusing the noun instance's UID in the spawned object's name — folds into the `Saveable` GUID work instead. |
| `SerializableSpawnerEditor` | Commented out wholesale in the fork. |
| xNode `NodeEditor.cs` | Fork comments out the entire `ODIN_INSPECTOR` drawing path. That's him disabling a feature, not fixing one. |
| `PropertyScriptableObject` | Fork comments out the `ReferenceList`. |
| `GenericRankedWeightedAction` + `ChooseGenericRankedWeightedActionNode` | Heavily Odin-attributed; largely duplicates the existing `RankedWeightedAction` with the noun input removed. Fix B7 instead. |
| Flat single-asmdef layout | The fork collapsed to one `Narramancer.asmdef` at plugin root with no separate Editor or XNodeEditor assembly. Divergence, not improvement. |

---

## Design signals — not code

Things the fork proves the plugin is missing, where the fork's own implementation isn't the answer:

- **Relationship stats.** `GetRelationshipStatNode` / `ModifyRelationshipStatNode` add a numeric value to
  a relationship — a genuine gap, since `RelationshipInstance` carries no data today. But the fork
  implements it by stashing a float on the *source noun's* blackboard under a synthesized
  `"{name} ({hash}) between {uid} with {uid}"` key. That's a workaround for a missing domain feature, and
  it embeds a `GetHashCode()` in a persisted key. If relationship stats are wanted, model them properly on
  `RelationshipInstance`.
- **Conditional verb selection.** `ChoosePrioritizedConditionalActionVerbNode` and
  `OfferActionVerbsAsChoicesNode` are the fork's most reused custom nodes and are genuinely general —
  pairs of (condition `ValueVerb`, effect `ActionVerb`), evaluated in priority order, re-evaluated on a
  refresh tick. Worth building natively, de-Odin'd, rather than porting verbatim.
- **An event/trigger entry point.** Sixty-odd game scripts fire Narramancer graphs from Unity callbacks,
  and the fork kept reaching for an event bus to do it. Today a graph can only be started by
  `RunActionVerbMonoBehaviour` on `Start`. `runOnEnable` / `stopOnDisable` (fork additions to that
  component) are the cheap 80% of this and are safe to take with B1.

---

## Recommended cut

| Phase | Content | Gate |
|---|---|---|
| **1. Now** | All of Tier A (A1 first — it's a compile failure for Odin owners). ~1 evening. | None. |
| **2. With §2** | B1, B2, B3 — the serialized-format changes. Land while breaking the save format is free. | Needs the test net. |
| **3. Alongside** | B4 → B5 → B7, then B6. Editor/authoring quality, no format impact. | B5/B6 need B4. |
| **4. Fold in** | B8 into the `Saveable` refactor; `SerializeNavMeshAgent` as the reference `IComponentSaver`. | Read `SAVEABLE_REFACTOR_HANDOFF.md` first. |
| **5. Optional** | Tier C generic nodes; B9, B10 as riders. | — |

Everything ported from Tier B/C needs the same two passes on the way in: **strip Odin Inspector
attributes**, and **move global-namespace types into `Narramancer`**.
