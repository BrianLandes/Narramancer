# Odin removal — implementation plan

> **Status:** plan only, nothing built. Execution is gated on the save→load round-trip test
> ([`../tasks.md`](../tasks.md) §2), which does not exist yet.
>
> **Scope:** replace the vendored `Narramancer.OdinSerializer` with a serializer we own, porting v2's
> `NarraSerializer` (`/home/brian/Projects/Narramancer2/Assets/Narramancer/Core/Serialization`, ~1,300 LOC).
>
> **Why it matters commercially:** a buyer who owns Odin Inspector hits a duplicate-assembly conflict, because
> both ship OdinSerializer. That's a conversion blocker in exactly the buyer segment most likely to pay.

---

## 0. What Odin is actually doing — read from a real save file

The two call sites in `SaveLoadUtilities` (`SerializeValue`/`DeserializeValue`, JSON only) understate the job
enormously. What matters is the **object graph** reachable from `StoryInstance`. Rather than infer it, this plan
was written against a real save produced by the verified Linux round-trip:

`~/.config/unity3d/Professional Bad Guys/Narramancer/Saves/SaveSlot_001.json` — 165 KB, of which 150 KB is the
base64 thumbnail and **14 KB is the actual Odin payload**.

### The wrapper

```json
{ "title": "...", "data": "<the Odin JSON>", "thumbnail": "<base64 jpg>",
  "objects": [ {"instanceID": 4206}, {"instanceID": 4222}, {"instanceID": 4204} ] }
```

`objects` is Odin's external-reference list, serialized by `JsonUtility`. **Hold that thought — see §2.**

### Odin's on-disk vocabulary (what we'd be replacing)

| Marker | Meaning | Count in this save |
|---|---|---|
| `$id` | object identity, assigned on first write | 38 |
| `$type` | `"N\|Full.Type.Name, Assembly"` on first use, then bare `N` — a per-document **type table** | 38 |
| `$iref:N` | internal reference to an already-written `$id` (shared/cyclic) | 8 |
| `$eref:N` | **external** reference — an index into the wrapper's `objects` list | 6 |
| `$rlength` / `$rcontent` | collection length + items | 14 |
| `$k` / `$v` | dictionary entry key/value | 5 |

### A second, much richer corpus — 40 real saves

`~/Exodus_2026/Unity Projects/*/Assets/Saves/` holds **40 further Narramancer saves across 6 projects**
(Cult Leader 19, SoC Packages 10, Shroom Tycoon 7, Show Surprise 2, Equipment System 1, My project 1). All 40
parse as the same wrapper. This is a far better test corpus than the sample scene, and it answers empirically
what §7 previously listed as an open question.

**Aggregate shape:** 66 distinct types, 10,752 `$id`s, 3,209 `$iref`s, 8,474 dictionary entries, and
**4,095 external Unity references across 2,608 object-list entries**.

Two things it revealed that the single sample save did not — both material, see §2.

> **Use these as golden files.** Any two of the largest (Cult Leader, Shroom Tycoon) exercise more of the
> format than any test we'd write by hand. Note that a few saves reference `SubPropertyInstance` and
> `PropertyRelationshipInstance`, which **do not exist in v1** — they're from the Pseudo World Gaia fork, so
> those particular files are for reference, not as pass/fail fixtures.

### What that tells us is in the graph

- **Polymorphism** everywhere — `AdjectiveInstance` subclasses, `StringObjectDictionary` holding arbitrary
  `object`, node types.
- **Shared references** — `$iref:5` reuses one `GenericEqualityComparer`; `$iref:9` links a `NodeRunner` from
  two places (the `nodeRunners` dictionary and a closure's captured field).
- **Unity object references** — `$eref` pointing at `PrintTextNode` assets (xNode nodes are ScriptableObjects).
- **`Dictionary<K,V>` internals leak in.** Odin reflects `Dictionary`'s own fields, so every dictionary in the
  save carries a serialized `comparer` object. Pure noise we get to drop.
- **A compiler-generated closure is serialized.** More on this in §2.

---

## 1. Feature inventory — what has to be reverse-engineered

| # | Feature | Exercised in v1 by | v2 `NarraSerializer` | Work |
|---|---|---|---|---|
| 1 | Reflected instance fields, whole inheritance chain | everything | ✅ `SerializationFields` | **Policy change — see below** |
| 2 | Polymorphic `$type` tags | adjectives, `object` blackboard, nodes | ✅ `TypeResolver` + `$type` | port as-is |
| 3 | Shared + cyclic references | `NodeRunner` ↔ closures ↔ dictionaries | ✅ `$id`/`$ref` | port as-is |
| 4 | `List<T>` + arrays | instances, timers, queued nodes, events | ✅ `$values` | port as-is |
| 5 | `Dictionary<K,V>` | blackboards, `nodeRunners`, `promises`, `flags` | ⚠️ exact `Dictionary<,>` only | **must walk the base chain** |
| 6 | Primitives + enums | everywhere | ✅ `Scalars` | widen (see below) |
| 7 | Unity structs (`Vector3`, `Quaternion`, `Color`) | `Saveable` driver state, tweens | ✅ falls out of field reflection | none |
| 8 | **`System.Type` values** | `SerializableType._type` (a `RuntimeType`) | ❌ | **new — or deleted entirely, see §2B** |
| 9 | **External Unity object references** | nodes, nouns, adjectives, sprites | ⚠️ `$assetRef` seam exists, no Unity impl | **new + a correctness fix** |
| 10 | **Compiler-generated closure classes** | `Promise.doneCallbacks` | ❌ never faced it | **avoidable — do §2B first and this disappears** |

### 1a. The field-selection policy is wrong for v1 — fix this first

v2's `SerializationFields` policy is *"every instance field that is public or `[NarraSerialize]`, unless
`[NonSerialized]`/`[NarraIgnore]`"*.

**v1 marks private fields with Unity's `[SerializeField]`.** `StoryInstance.instances`,
`NodeRunner.runningNode`, `Blackboard.ints` — all `[SerializeField] private`. Ported verbatim, v2's policy
**silently skips almost the entire save** and produces an empty-looking payload that still parses. This is the
single most dangerous line in the port, because it fails quietly rather than throwing.

The v1 policy must be: **public, or `[SerializeField]`, or `[NarraSerialize]` — minus `[NonSerialized]`.**
`[NonSerialized]` is already load-bearing in v1 and must keep working: `Blackboard.gameObjects` /
`.components` and `NodeRunner.lastRunningNode` are deliberately excluded, and the real save confirms Odin
honors that.

### 1b. `SerializableDictionary` derives from `Dictionary<K,V>`

`CollectionInfo.TryDictionary` tests `GetGenericTypeDefinition() == typeof(Dictionary<,>)` — an exact match.
v1's `StringNodeRunnerDictionary : SerializableDictionary<string, NodeRunner> : ... : Dictionary<string, NodeRunner>`
fails that test, falls through to field reflection, and serializes the `[SerializeField] keys`/`values` backing
arrays *plus* `Dictionary`'s internals. It would probably still round-trip, but via the wrong path and at
roughly double the size.

Fix: walk `BaseType` looking for a `Dictionary<,>` (same for `List<>`). Then treat it as `IDictionary` and
**ignore** the `[SerializeField]` backing arrays entirely — `ISerializationCallbackReceiver` is Unity's
mechanism, irrelevant to us, and populating the dictionary directly is correct.

### 1c. Widen `Scalars`

v2 handles `bool/int/long/float/double/string/enum`. Audit v1 for `byte`, `char`, `uint`, `ulong`, `short`,
`decimal`, `DateTime`, `Guid`. Cheap to add; the failure mode is a hard `NotSupportedException`, so it's
self-announcing rather than silent.

---

## 2. The two landmines

### Landmine A — Unity object references are `instanceID`s, and that is not save-safe

This is the most consequential thing found while writing this plan, and it is **a pre-existing bug, not
something the port introduces.**

Odin hands out `$eref:N` indices into a `List<UnityEngine.Object>`, which `JsonUtility` then writes as:

```json
"objects": [ {"instanceID": 4206}, {"instanceID": 4222}, {"instanceID": 4204} ]
```

An `instanceID` is **assigned at load time by the running Unity process.** It is not a persistent asset
identity. What this means in practice:

- **Same build, quit and relaunch → usually works.** Assets load in a deterministic order, so IDs tend to come
  back the same. This is exactly the scenario Task Z verified, and why it passed.
- **Ship a patch, and saves can break.** Add, remove, or reorder assets and the IDs shift. Every `$eref` then
  resolves to the wrong object or none — a save that silently loads the wrong node into a resumed verb.
- **In the editor it is not stable at all.**

**Scale, from the 40-save corpus:** the sample scene's save has 3 external references. Real project saves carry
**4,095 across 2,608 object-list entries** — averaging ~65 session-local asset pointers per save, peaking far
higher. The exposure isn't a corner case; it's most of what a real save points at.

For a plugin whose headline feature is save/load, "your players' saves break when you patch your game" is the
worst possible failure. **The replacement must key Unity references by asset GUID**, not instanceID.

#### This is a *different* GUID from the `Saveable` component's — both are needed

Easy and expensive to conflate, so state it plainly. There are **two** identity problems:

| | Question it answers | Identity | Where it lives |
|---|---|---|---|
| **Scene-object identity** | "Which GameObject does this saved state belong to?" | a minted GUID | `Saveable` component — see [`SAVEABLE_REFACTOR_HANDOFF.md`](SAVEABLE_REFACTOR_HANDOFF.md) |
| **Asset identity** (Landmine A) | "Which *asset* does this saved reference point at?" | Unity's **asset GUID**, already in the `.meta` file | a new `UnityReferenceResolver` |

The `Saveable` GUID does **not** help here, because none of the `$eref` targets are scene objects. They are
project assets: `PrintTextNode` ScriptableObjects (xNode nodes *are* assets), `NounScriptableObject`,
`PropertyScriptableObject`, sprites.

**Proof that it's 100% assets:** `Blackboard.gameObjects` and `Blackboard.components` are both
`[NonSerialized]` — scene references are deliberately never written to a save. So every one of the 4,095
external references is an asset reference.

Mechanism: `AssetDatabase.AssetPathToGUID` / `GUIDToAssetPath` in the editor, and a **baked manifest**
ScriptableObject in builds, where `AssetDatabase` doesn't exist. **v1 already has the hook** —
`SingletonBuildPreprocessor : IPreprocessBuildWithReport` calls `NarramancerSingleton.OnPreprocessBuild()`, so
there is an existing build-time seam to populate the manifest from. The singleton already holds references to
the noun/verb catalogue, so part of the manifest may fall out of what's there.

v2 has the right seam on the Core side already: `IReferenceResolver` (`TryResolve(id, type, out asset)` /
`TryGetId`) plus the `$assetRef` handle. Only the Unity implementation is missing.

> **Worth proving before building anything.** Save in a build; add a dummy asset; rebuild; load the old save.
> If it breaks, that's the bug demonstrated and this stops being a refactor and becomes a fix. One evening.

#### One symptom, three causes

"Saves break when the build or the scene changes" is a single observed symptom with three independent causes.
All three are now tracked, and none of them fixes another:

| Trigger | Cause | Fix |
|---|---|---|
| The **scene** changed | save keys are `path + component index` | `Saveable` GUID |
| The **build** changed | asset refs are session-local instanceIDs | asset GUID (Landmine A) |
| A **node's source file** changed | closure type names are compiler ordinals | Landmine B |

### Landmine B — the continuation is a serialized C# closure

`Promise.doneCallbacks` is a `List<SerializableAction>`, and the save contains this:

```json
"_object": { "$type": "Narramancer.PrintTextNode+<>c__DisplayClass22_0, Narramancer",
             "<>4__this": $eref:2, "runner": $iref:9 }
```

That is a **compiler-generated lambda closure** from `PrintTextNode`, serialized by type name. There are
**14 `WhenDone(() => ...)` sites** across the node library, so ~14 such classes.

**The corpus proves the fragility is real, not theoretical.** Across the 40 saves plus this repo's,
`PrintTextNode` has serialized closures under **three different ordinals**:

| Save origin | Type written |
|---|---|
| this repo, 2026-08-11 | `PrintTextNode+<>c__DisplayClass22_0` |
| other projects (14 saves) | `PrintTextNode+<>c__DisplayClass1_0` |
| other projects (2 saves) | `PrintTextNode+<>c__DisplayClass19_0` |

Same class, same purpose, three names — because the suffix is a **method ordinal assigned by the compiler**,
and it moved as the source file changed. `OfferObjectsAsChoicesNode` shows the same drift (`13_0` and `36_0`).
A save written before an edit to the node's source cannot resolve its continuation afterwards. **Editing a node
file can silently invalidate saved games**, and nothing warns you.

Seven distinct closure types appear in the corpus, plus two occurrences of a bare **`System.Action`** — a raw
delegate serialized directly, which is worse still.

#### What the callbacks actually are — this reframes the whole problem

The lambdas look general. They aren't. Every one is a one-liner, and the captured state is nearly always just
"which runner":

```csharp
// WaitNode        — captures: runner
.WhenDone(() => { runner.Resume(); });

// PrintTextNode   — captures: runner, waitForContinue
textPrinter.SetText(inputText, () => { if (waitForContinue) runner.Resume(); }, ...);

// ParallelNode    — captures: subRunner
.WhenDone(() => { NarramancerSingleton.Instance.ReleaseNodeRunner(subRunner); });
```

**We are not serializing arbitrary functions — we're serializing a handful of fixed intentions.** Across the 14
sites there are perhaps four or five distinct kinds ("resume this runner", "release this sub-runner", "resume
if a flag"). That makes the problem far smaller than general delegate serialization.

#### Recommended fix: a `Continuation` record

Store a **stable identity for the code** plus its **arguments as data** — the durable-workflow / job-queue
pattern. What can't be serialized safely is a *closure*, because its identity is compiler-generated; a named
intention plus data has no such problem.

This collapses neatly in v1 because runners are **already** addressed by string key in `story.NodeRunners`:

```csharp
[Serializable]
public class Continuation {
    public enum Kind { ResumeRunner, ReleaseSubRunner, ResumeIfFlag }
    public Kind kind;
    public string runnerKey;   // into story.NodeRunners — already how runners are addressed
    public bool flag;          // the occasional captured primitive
}
```

`Promise.doneCallbacks` becomes `List<Continuation>`. What that **deletes**:

- the entire `SerializableActionHelper` plugin — `SerializableAction`, `SerializableMethodInfo`,
  `SerializableObject`, `SerializableObjectOneLevel`/`TwoLevel`, `SerializableType`
- serializer feature **#8 (`System.Type` support)**, needed only by `SerializableType._type`
- serializer feature **#10 (closure reflection)** entirely
- the `System.RuntimeType` and bare `System.Action` entries from the type inventory

So it removes a vendored plugin *and* shrinks the serializer being ported. Roughly 1–2 evenings, independent of
the serializer, and **cheapest done first** — then `NarraSerializer` never needs `System.Type` or
compiler-generated-field support at all.

#### Options considered and rejected

**Adopt v2's position deque.** v2 deleted this problem rather than solving it: no `Promise` or
`SerializableAction` exists there. Continuations are a deque of `RunnerPosition { GraphId, NodeId }`, and
`Finished` is a plain non-serialized C# event. Correct, but it's `NodeRunner` surgery and the top of the slope
back into the paused rewrite. **The `Continuation` record gets most of the benefit for a fraction of the risk.**

**Named parameterless methods** (`promise.WhenDone(this.OnDone)`). Doesn't actually work on its own: the
lambdas capture *locals* (`runner`, `waitForContinue`) and `Action` takes no parameters, so the captured state
still needs a serializable home. Once you build that home you've built `Continuation` — so go there directly.

**Fuzzy-match the closure by shape.** Search the outer type's nested compiler-generated classes and match on
field names (`<>4__this`, `runner`, `waitForContinue`). **Rejected** — it fails precisely where it matters:

- `OfferChoicesNode` emits `<>c__DisplayClass3_0` *and* `3_1` in the same save. Two lambdas with identical
  capture shapes are indistinguishable.
- Having guessed the class, you must still choose between `<Run>b__0` and `b__1` — the same ambiguity again.
- It converts a loud failure into a silently **wrong** callback. For a save system that is strictly worse.
- These names are documented compiler implementation details; no attribute or flag stabilizes them.

#### Until it's fixed: validate at load, not at invoke

Correcting an earlier claim in this doc: an unresolvable closure type does **not** degrade to a null callback.
`SerializableType.Deserialize` does `throw new Exception("Could not deserialize type ...")`. But it throws
**lazily** — from the `Action` getter, at *invoke* time — so the failure surfaces when the player triggers the
continuation, long after the load that caused it. If the closure model survives into the port for any length of
time, validate every callback type at **load** time and reject the save with a clear message there.

---

## 3. What gets ported vs written

**Port nearly as-is** (~900 LOC): `Json/JsonParser`, `Json/JsonWriter`, `Json/JsonValue`, `NarraWriter`,
`NarraReader`, `ReferenceEqualityComparer`, `SerializationException`, `NarraSerializeAttribute`,
`NarraIgnoreAttribute`, `TypeResolver`, `TypeVocabulary`.

**Port with changes**: `SerializationFields` (§1a), `CollectionInfo` (§1b), `Scalars` (§1c).

**Write new**: a `UnityReferenceResolver : IReferenceResolver` (GUID ↔ `UnityEngine.Object`, editor via
`AssetDatabase` + a baked manifest for builds), `System.Type` handling, and the `SaveLoadUtilities` swap.

**Also port**: v2's serializer tests — `ObjectGraphSerializerTests`, `NarraSerializerTests`,
`ReferenceHandleSerializationTests`, `ReferenceSerializerTests` already exist in
`Narramancer2/dotnet/Narramancer.Core.Tests/Serialization/`. Free coverage; adapt to the v1 field policy.

**Namespace note**: v2 lives in `Narramancer.Core.Serialization` and uses the `Narra*` prefix. v1 has no
`Narramancer.Core` assembly and doesn't use that prefix — decide on the way in (suggest keeping the v2 names
inside a `Narramancer.Serialization` namespace, so a future Core extraction is a namespace move, not a rename).

---

## 4. Phasing

Each phase leaves the build green; Odin stays until the last one.

**Phase 0 — prove Landmine A** *(~1 evening)*. Save in a build, add an asset, rebuild, load. Establishes
whether GUID references are a fix or just a nicety. Do this first — it sizes everything else.

**Phase 1 — the round-trip test net** *(gate, already in `tasks.md` §2)*. Non-negotiable. Author a story, run
to a suspend point, save, deserialize, assert state and resume. Without it there is no way to know the new
serializer is faithful.

**Phase 1b — replace closures with `Continuation`** *(~1–2 evenings, §2B)*. Independent of the serializer, and
cheapest here: doing it before the port means features #8 and #10 never have to be built. Deletes the
`SerializableActionHelper` plugin. Skippable if you'd rather port first — but then build #8 and #10, and add
load-time callback validation.

**Phase 2 — port the engine-agnostic core** *(~2 evenings)*. Json + writer/reader + type resolver, with the
three modifications from §1. Port v2's tests alongside. No v1 wiring yet — it compiles and tests standalone.

**Phase 3 — type inventory + `UnityReferenceResolver`** *(~2 evenings)*. Enumerate every type reachable from
`StoryInstance` and assert each round-trips. Build the GUID resolver and the baked manifest.

**Phase 4 — the swap** *(~1 evening)*. Replace the two `SerializationUtility` calls. Ship `schemaVersion` in
the envelope from day one and fail with a clear "incompatible save" message rather than a crash. Replace
`SaveDataWrapper.objects` (the instanceID list) with GUID handles.

**Phase 5 — delete Odin** *(~1 evening)*. Remove `Assets/Narramancer/Plugins/OdinSerializer` (186 files) and
its asmdef; drop the Odin entry from `Third-Party Notices.txt` and `LICENSING.md`; re-run the Task Z build
smoke on IL2CPP **and** WebGL, since serialization changed wholesale.

Roughly **7–9 evenings** after the test net, assuming the closure model is kept.

---

## 5. How we'll know it worked

1. **The round-trip test** (Phase 1) passes against the new serializer.
2. **Golden-file check against the 40-save corpus** (§0): deserialize each existing Odin save with the old path,
   re-serialize with the new one, and compare the object graphs field-by-field. Cheaper and far more convincing
   than eyeballing JSON, and it covers 66 types rather than the ~20 a hand-written test would reach. Exclude the
   few fork-only files (`SubPropertyInstance`, `PropertyRelationshipInstance`).
3. **The patch test**: save → add an asset → rebuild → load. Must pass, and it's the one that fails today.
4. **Task Z build smoke** on IL2CPP and WebGL — mandatory, since AOT is exactly where a reflection-based
   serializer breaks and no EditMode test can see it.
5. **Odin Inspector coexistence**: install Odin Inspector in a scratch project alongside the plugin and confirm
   there's no duplicate-assembly error. This is the actual commercial goal — worth verifying directly rather
   than assuming.

---

## 6. Decisions needed

1. **Continuation model — keep closures, or adopt v2's position deque?** Recommend **keep** (§2B). Changing it
   is execution-model surgery and the on-ramp to the paused rewrite.
2. **Build-time reference resolution — baked manifest, or Addressables?** Recommend a baked manifest
   ScriptableObject; Addressables is a dependency this plugin shouldn't take on.
3. **Namespace** — `Narramancer.Serialization` (suggested) vs matching v2's `Narramancer.Core.Serialization`.
4. **`Continuation` record — before the port, or skip it?** Recommend **before** (§2B). ~1–2 evenings, it
   deletes a vendored plugin, and it removes two features from the serializer being ported rather than adding
   to it. It's also another "free only while there are no users" change: editing a node file currently
   invalidates saves silently.
5. **Does the `Saveable` refactor land before or after this?** They both touch the save format and both are
   free only in the zero-user window. Driver `StateType`s are deliberately simple structs, so the ordering is
   flexible — but doing the serializer first means the `Saveable` work is written against its final format.

## 7. Open questions

- ~~Full type inventory reachable from `StoryInstance`~~ — **answered** by the 40-save corpus: 66 distinct
  types, listed by frequency in the §0 analysis. Notably beyond the sample scene: `UnityEngine.Rect`/`Vector2`/
  `Color`/`Quaternion`/`Vector3`, `ChoicePrinter+VisibleChoice`, `SerializeRectTransform+SerializedRectTransform`,
  `List<ScriptableObject>`, `List<object>`, `System.Type[]`, and a bare `System.Action`.
- Does anything rely on Odin's `$type` **numeric table** for size? The payload is 14 KB; even a 2–3x expansion
  is irrelevant next to the 150 KB thumbnail.
- `SerializableTimer`, `NounUID`, `Flag` (used as a *dictionary key*) — key types need stable equality under the
  new serializer, same class of concern as `NounInstancesQuery` in the Tier A fixes.
