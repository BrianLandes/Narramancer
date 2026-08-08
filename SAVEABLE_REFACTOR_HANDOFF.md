# Handoff: single `Saveable` component + drivers (port the v2 model to v1)

**Goal:** replace v1's zoo of `Serialize*` MonoBehaviours (one per Unity component type) with **one `Saveable`
component per object** that drives a **registry of savers ("drivers")**. Adding a GameObject to the save
system becomes "drop one component"; adding support for a new component type becomes "write one driver," not
"write a new MonoBehaviour + remember to place it."

This is v2's `GAMEOBJECT_SERIALIZATION_SPEC` model — but **scoped for v1**: keep v1's existing save pipeline,
swap only the *component granularity*. Don't rebuild the pipeline.

## What v1 does today (and what we keep vs replace)
- **`ISerializableMonoBehaviour`** (`Serialize(StoryInstance)` / `Deserialize(StoryInstance)`) — **KEEP** as the
  registry contract.
- **`NarramancerSingleton.monoBehaviourTable`** + `Register`/`Unregister`, and the save orchestration
  (`PrepareStoryForSave` iterates the table → `Serialize`; `LoadStory` reloads the scene by build index →
  `Deserialize` each) — **KEEP.** The new `Saveable` plugs into this unchanged.
- **`StoryInstance.SaveTable` (a `Blackboard`, Odin-serialized)** as the key→value store — **KEEP.** Drivers
  write their captured state here.
- **`SerializableMonoBehaviour`** base + the N concrete `Serialize*` components (Transform, RectTransform,
  Active, Animation, AudioSource, Image, SpriteRenderer, Text, NounInstanceReference) + `SerializableSpawner`
  — **REPLACE** with `Saveable` + drivers.
- **Identity:** today `Key = "{field} {transform.FullPath()}[{componentIndex}]"` — **REPLACE** with a stable
  GUID (rename/reparent/reorder-safe). **✅ DECIDED — see the "GUID identity" section below.**
- **`[SerializeMonoBehaviourField]`** (reflection over behavior-state fields, e.g. a tween's progress) — **KEEP
  the mechanism**, renamed/repurposed as the `[Save]` field capture (see below).

## New architecture (three pieces)

### 1. `Saveable : MonoBehaviour, ISerializableMonoBehaviour` — one per object
- `[SerializeField] private string guid;` — the **stable id**, minted in `Reset()`/`OnValidate()` at author
  time (scene objects bake it into the scene; runtime spawns mint one on `Awake` if empty).
- A **capture profile**: either an explicit list of drivers, or **auto-detect** — on `Awake`, find the
  components on this GameObject that have a registered driver. (Lean: auto-detect + an inspector list showing
  what will be saved, with per-entry opt-out.)
- Registers with the singleton in `Awake` (`NarramancerSingleton.Instance.Register(this)`), unregisters in
  `OnDestroy` — exactly like the old components.
- `Serialize(StoryInstance story)`: for each applicable driver, `state = driver.Capture(component)` →
  `story.SaveTable.Set(Key(driver.Key), state, driver.StateType)`. Then scan sibling components for `[Save]`
  fields and store them too.
- `Deserialize(StoryInstance story)`: reverse — read each driver's state and `driver.Apply(component, state)`;
  restore `[Save]` fields.
- **`Key(string driverKey) => $"{guid}:{driverKey}"`** — guid-based, not path-based.

### 2. `IComponentSaver` (the "driver") + registry
```csharp
public interface IComponentSaver {
    System.Type ComponentType { get; }   // e.g. typeof(Transform)
    string Key { get; }                  // stable per-driver key, e.g. "transform"
    System.Type StateType { get; }       // the serializable state struct (for SaveTable.Set/Get)
    object Capture(Component component);          // read live component -> state
    void Apply(Component component, object state);// write state -> live component
}
```
- A `ComponentSaverRegistry` maps `ComponentType → IComponentSaver`. Populated at startup with the built-in
  drivers; **third parties can register their own** (the extensibility win).
- Each driver is the extracted *save* logic from the matching v1 `Serialize*` component (see the port table).

### 3. `[Save]` attribute for behavior-state fields (the decoupled hook)
- v1's `Serialize*` components mixed two jobs: capturing the **component's state** (transform position) *and*
  storing **behavior state** (tween progress via `[SerializeMonoBehaviourField]`). **Split them:** drivers own
  component state; `[Save]`-annotated fields on any behavior component own behavior state. The `Saveable`
  scans its GameObject's components for `[Save]` fields and round-trips them (reuse the existing
  `[SerializeMonoBehaviourField]` reflection — rename to `[Save]`). This mirrors v2's "hooks decoupled from
  savers."

## Driver port table (extract each `Serialize*` → a driver)
| v1 component | New driver | Captures |
|---|---|---|
| `SerializeTransform` | `TransformSaver` | position / rotation / localScale (**tween logic moves to a behavior component with `[Save]` fields**) |
| `SerializeRectTransform` | `RectTransformSaver` | UI rect transform |
| `SerializeActive` | `ActiveSaver` | `gameObject.activeSelf` (**driver keyed on the Saveable itself**, not a component) |
| `SerializeAnimation` | `AnimationSaver` | animation state |
| `SerializeAudioSource` | `AudioSourceSaver` | audio source state |
| `SerializeImage` | `ImageSaver` | sprite / color |
| `SerializeSpriteRenderer` | `SpriteRendererSaver` | sprite / color |
| `SerializeText` | `TextSaver` | text |
| `SerializeNounInstanceReference` | `NounBindingSaver` | the GameObject↔`NounInstance` link (keep v1's approach; a Saveable option "represents a noun") |
| `SerializableSpawner` | *keep as-is for now* | spawned prefabs carry a `Saveable`; full spawn/tombstone delta is **deferred** (see scope) |

## GUID identity (✅ decided — the robustness upgrade)
Replace the path+index+field `Key()` across the whole `ISerializableMonoBehaviour` save system with a **stable
per-GameObject GUID**. This is the main reason the old system was brittle: renaming an object, reparenting it,
reordering components, or two same-named siblings all broke the key. The GUID is immune to all of that.

**Where it lives:** one `[SerializeField] private string guid;` on `Saveable` (the single guid holder per
GameObject). Every key becomes `$"{guid}:{discriminator}"` — driver key for component state, `componentType +
fieldName` for `[Save]` fields.

**Minting + the gotchas (get these right — they're the classic failures):**
1. **Mint at author time:** in `Reset()` (fires when the component is added) and as a safety net in
   `OnValidate()` when the guid is empty. Use `System.Guid.NewGuid().ToString("N")`.
2. **Duplicate-paste detection (critical):** copy-pasting a GameObject in the editor copies the serialized
   `guid` → two objects with the **same** guid. On `OnValidate()` (editor) and `Awake()` (play), if another
   live `Saveable` already claims this guid and it isn't me, **re-mint**. Keep a runtime
   `Dictionary<string, Saveable>` of claimed guids for O(1) detection; in the editor, a paste triggers
   `OnValidate` on the copy, which sees the collision and re-mints.
   > **Prior art to copy:** Unity's open-source **`GuidComponent`** (the "guid-based-reference" sample) solves
   > exactly this — serialized guid + editor duplicate detection. Lean on its approach rather than reinventing.
3. **Prefabs:** don't bake a guid onto the **prefab asset** (all instances would share it). Leave it empty on
   the asset; each **scene instance** / runtime spawn mints its own (Reset on placement / Awake on spawn +
   the duplicate check).
4. **Two object populations:**
   - **Authored scene objects (the main win):** guid is baked into the scene at author time → stable across a
     save/scene-reload, so keys match on `Deserialize`. This is what fixes the brittleness.
   - **Runtime-spawned objects:** the guid is minted at runtime, so it **must be saved and re-applied** to the
     re-spawned object on load — else the re-spawn mints a *different* guid and its saved state doesn't match.
     v1's `SerializableSpawner` already re-spawns + re-deserializes its children on load; extend it to also
     record and **reassign each spawn's guid** on re-spawn. *(This is the fiddly case — see scope: land
     authored-object GUIDs first, spawned-object guid reassignment as the second step.)*

**Extend to the runner-carrying savers (recommended, same rework):** `RunActionVerbMonoBehaviour` and
`NarramancerScene` are also `SerializableMonoBehaviour`s today, keyed by path — and they're exactly the
objects whose stable identity matters most (they hold in-flight `NodeRunner`s that must resume after load).
Migrate them to the guid model: give their object a `Saveable`, and capture their `NodeRunner`/`Promise`/state
via the `[Save]` scan keyed off the sibling `Saveable`'s guid. **Preserve v1's special-casing:** `NodeRunner`
and `Promise` fields still route to `story.NodeRunners[...]` / `story.Promises[...]` side tables — just keyed
by `guid` now instead of path. *(If time-boxed, these can stay path-keyed in the first cut, but that leaves
identity inconsistent — prefer migrating them together.)*

**Break freely:** ~0 users → the key-format change needs no save migration.

## Scope for v1 — what to build vs defer
**Build (the win the user asked for):**
- `Saveable` (single component) + stable GUID + auto-detect profile.
- `IComponentSaver` + registry + the ~9 built-in drivers above.
- `[Save]` behavior-field capture (rename of `[SerializeMonoBehaviourField]`).
- Reuse the existing singleton registry + `SaveTable` + scene-reload-on-load orchestration.

**Defer (v2 has these; v1 doesn't need them for this win — note, don't build):**
- The **engine-agnostic Core `SceneSnapshot` POCO** — v1 isn't engine-agnostic; the existing `Blackboard`
  save table is fine.
- The **full spawned/destroyed tombstone delta** — v1 already reloads the scene on load + keeps the spawner,
  which covers the common cases. Revisit only if it bites.
- The **scene registrar / noun-binding table refactor** — keep v1's `NounInstanceReference` approach as a driver.
- **`TypeCache`-based auto-registration of drivers** — nice-to-have; a hardcoded registry is fine to start
  (can adopt the `AssemblyUtilities`→`TypeCache` work later).

## Migration (v1-specific)
- **~0 installed users → break the save format freely now** (same zero-user-window logic as the Odin swap).
- Update the **sample scenes/prefabs**: remove `Serialize*` components, add one `Saveable` per object
  (auto-detect covers most). Optional editor helper: "scan scene → replace `Serialize*` with a configured
  `Saveable`," but for a handful of sample scenes, doing it by hand is fine.
- Keep `SerializableSpawner` working; ensure spawned prefabs carry a `Saveable`.

## Interplay with the Odin removal (Item #1)
Driver state is written into the `Blackboard` `SaveTable`, which is serialized by Odin now / `NarraSerializer`
later. **Keep driver `StateType`s simple, serializer-friendly structs** (plain fields: Vector3, Quaternion,
Color, string, sprite refs as resolvable handles) so they survive the serializer swap unchanged. The two
refactors are independent but both touch the save format — do them close together in the zero-user window.

## Testing (EditMode)
- Per driver: put a `Saveable` on an object with the component, `Capture` → mutate the component → `Apply` →
  assert restored (transform position, active state, sprite, text, …).
- Round-trip through the save table: `Serialize` → new scene/instance → `Deserialize` → state matches.
- GUID stability: rename / reparent / reorder components → the guid (and thus the keys) are unchanged.
- **GUID duplicate detection:** duplicate a `Saveable` GameObject → the copy gets a **new, distinct** guid
  (not the original's). Two objects never share a guid.
- **GUID prefab:** the prefab asset carries no guid; two instances of the same prefab get distinct guids.
- **Spawned-object guid (second step):** spawn → save → reload → the re-spawned object's saved state matches
  (guid reassigned by the spawner, not re-minted).
- `[Save]` field: a behavior component's annotated field round-trips; a `NodeRunner` field routes to the
  side table under the guid key and resumes after load.
(These feed the Phase-1 test net in `V1_IMPROVEMENT_PLAN.md`.)

## Naming
v1 doesn't use the `Narra*` prefix (that's a v2 convention). Match v1: either **`Saveable`** with
`[AddComponentMenu("Narramancer/Saveable")]` (short, menu-branded — matches the naming-conventions rule), or
**`NarramancerSaveable`** (collision-safe, matches `NarramancerScene`/`NarramancerSingleton`). Recommend
`Saveable` + the menu attribute; fall back to `NarramancerSaveable` if the bare name collides in a real project.

## Build order (suggested)
1. `Saveable` + **GUID identity** (mint / duplicate-detect / prefab) + reuse the singleton registry.
2. `IComponentSaver` + registry + the built-in drivers (port the `Serialize*` table).
3. `[Save]` behavior-field scan (rename `[SerializeMonoBehaviourField]`), incl. `NodeRunner`/`Promise`
   side-table routing under the guid key.
4. Migrate the runner-carrying savers (`RunActionVerbMonoBehaviour`, `NarramancerScene`) to the guid model.
5. Spawned-object guid reassignment in `SerializableSpawner`.
6. Update the sample scenes; EditMode tests.

## Open questions
- Auto-detect vs explicit driver list on `Saveable` (lean auto-detect + inspector opt-out).
- `ActiveSaver` and any other non-component captures (gameObject active, layer, name?) — keyed on the Saveable.
- ✅ **GUID identity: DECIDED — do it now** (author-time mint + duplicate detection; see the GUID section).
  Authored-object GUIDs are step 1; spawned-object guid reassignment is step 5.
- Reference: the full design + rationale is v2's `docs/GAMEOBJECT_SERIALIZATION_SPEC.md` (in the narramancer2
  repo) — this handoff is the v1-scoped subset.
