# Done

Archive of completed tasks and finished phases, moved here from
[`tasks.md`](tasks.md) so the active list stays short. Newest at the top.

## How to use this file

- When a task in `tasks.md` is finished, **move** its line here (don't just
  check it off and leave it there). Keep it in the **same commit** as the work.
- Change the checkbox to `- [x]` and, when useful, append a short parenthetical
  note for the future: what shipped, where it lives, or a gotcha worth knowing.
- Group related items under a dated `## Completed <YYYY-MM-DD>` heading when you
  finish a batch, so the archive reads as a changelog.
- Never let this file gate anything — it's a record, not a work queue. If a
  "done" item turns out to need follow-up, open a fresh task in `tasks.md`
  rather than un-archiving.

---

## Completed 2026-10-27

- [x] **A load no longer resets — or crashes on — a component the save knew nothing about.** Found while
  proving the new spawner tests could fail: with no saved entry, `SerializeTransform` threw a
  `NullReferenceException`, which inside `LoadStory`'s loop **abandoned the load for every component after
  it**. The siblings failed quieter: `SerializeActive` hid its object (a missing bool reads back `false`),
  `SerializeText` blanked its text, `SerializeAudioSource` zeroed volume and pitch, `SerializeImage` went
  transparent, `CreateNounForGameObject` threw too. This is the **patch case** — an object added to a scene
  after a save was written — so it sat directly under the feature the patch test exists to prove.
  - Fixed once, in `SerializableMonoBehaviour`: `Serialize` writes a `$saved` marker, and
    `valuesOverwrittenByDeserialize` is only set when `Deserialize` finds it. Every override returns early
    without it, so the object keeps its scene state and initializes as on a fresh start. Base-class fields
    missing from the save now keep their initializers instead of becoming their type's default.
  - New `Blackboard.Contains(key, type)` — `Get` cannot tell "saved as the default" from "never saved".
  - ⚠️ **Format break, deliberately free:** saves written before this have no marker, so they restore no
    scene-component state. Zero users; only local test saves are affected.
- [x] **`ChoicePrinter` / `TextAndChoicePrinter` filed choices under the literal key `"choices"`** — not
  `Key(...)`, so M4a's identity work never reached them. Any two printers overwrote each other's saved choices,
  and the two types shared the literal, so they collided with each other too. Now guid-keyed.
- [x] **`Blackboard.Set(key, null)` no longer throws** — reachable from any unconnected object input via
  `NarramancerPort.AssignValueFromNodePort`. A typeless null now clears the key from every reference table.
  Also `Remove<TEnum>` looked for enums among objects although `Set`/`Get` file them as ints, so it never
  removed one.
- [x] **Noun-instance init order (Tier B8)** — `CreateNounForGameObject` is `[DefaultExecutionOrder(-100)]` and
  gains an idempotent `EnsureInstance()`; `SerializeNounInstanceReference.GetInstance()` creates the noun
  instead of answering null when asked before `Start`. Skipped the fork's `randomizeUid` (a feature, not the fix).
- [x] **`NarramancerScene` runner keys — not a bug after all.** The 2026-10-27 docs pass filed it as one;
  a test showed otherwise. Its runners are named by verb name, but that name is a label (`CreateNodeRunner`
  de-duplicates it), and the scene's list and the story's runner table come back as the *same* objects after
  a round trip. Nothing looks a runner up by that name.
- [x] **Test coverage for the scene components** — `SceneComponentSaveTests` (12) plus 3 in `BlackboardTests`:
  the spawner putting guids back on re-spawn (one and two spawns), runner identity for `NarramancerScene` and
  `RunActionVerbMonoBehaviour`, missing-state handling, printer keys, noun init order. Each fix was reverted
  individually and its test seen to fail with the original symptom. 530/530 passing.

## Completed 2026-09-04

- [x] **Serialized closures replaced with a `Continuation` record (M3).** A suspended verb's continuation was a
  C# closure, serialized by reflecting the compiler-generated class. The ordinal in `<>c__DisplayClass22_0` is
  assigned by the compiler and **moves when the source file is edited** — `PrintTextNode` had been saved under
  three different ordinals across real saves — so editing a node could silently invalidate saved games.
  All **18 sites across 3 storage mechanisms** converted; `SerializableActionHelper` deleted.
  Design: [`docs/CONTINUATION_DESIGN.md`](docs/CONTINUATION_DESIGN.md).

  **Verified on a real WebGL player, not just in the editor:** saved mid-choice, mid-`Wait` and mid-`WaitUntil`,
  closed the tab, reloaded, and resumed correctly each time; a sub-verb given 4 seconds of a 12-second job was
  cancelled cleanly. All 7 polling-node characterization tests written on 2026-08-27 still pass, which is what
  they existed for.

  *Decisions worth keeping:*
  - **`WhenDone(Continuation)` and `WhenDoneThisSession(Action)` are different names, not overloads.** A lambda
    binds to an `Action` parameter silently, so an overload let all 18 call sites keep compiling while quietly
    losing the ability to survive a save — a green build and a broken save system. Renaming turned it into 14
    compile errors, which became the checklist.
  - **A closure is now refused, not merely unsupported.** Without the old carve-out its fields are neither
    public nor `[SerializeField]`, so it would serialize as an *empty object* and lose captured state
    silently. That is how this bug class returns, so the writer throws and names what to use instead.
  - **`$systemType` support was kept**, against the plan. Nothing needs it, but it is written, tested, free at
    runtime, and a blackboard can legitimately hold a `Type`.
  - `PrintTextNode`'s `waitForContinue` left the save format entirely — it passes *no* continuation when
    false rather than encoding a flag to check.

- [x] **Two bugs the M3 smoke build found that no editor test could.**
  - **Saved timers were anchored to the previous session's clock.** `SerializableTimer` stored
    `Time.time + duration`, and `Time.time` resets on relaunch with nothing rebasing it, so every saved wait
    was delayed by however long the *previous* session had been running. A 4-second cancellation took twelve.
    Now stores `secondsRemaining` and counts down. **`Time.time` is 0 in EditMode, so no editor test can tell
    the two forms apart** — this was only ever visible in a player. Predates M3.
  - **Timers had no owner, so cancelling a node did not stop them.** Filed as cosmetic; it was not. A
    `Continuation` names its runner *by key*, and a released runner's name becomes available again, so a
    stale timer could resume a completely unrelated runner that happened to reuse the name. Proved by
    disabling the fix and watching the test fail.

- [x] **Execution-model correctness (M2).** A runner now records the runners it starts and `StopAndReset`
  stops them depth-first — stopping a runner mid-flight used to abandon everything below it, still ticking
  against a blackboard nobody owned. Ownership is *recorded, never recomputed*: both existing `Cancel`
  overrides had rebuilt a sub-runner's name from hash codes, which after a reload made
  `ChoosePrioritizedBranchNode` silently fail to cancel and `ParallelNode` throw.
  Also: **`Blackboard.UniqueKey` was the instanceID bug in a second place** — `UnityEngine.Object.GetHashCode()`
  *is* `GetInstanceID()`, across **13 call sites in 9 node types** (loop indices, spawned objects, timers,
  chosen elements), all silently lost when the build changed. Plus `InequalityNode`'s operators (`GreaterThan`
  evaluated `>=`) and 18 regression tests for the Tier A fixes.

  *Four task premises turned out to be stale*, all from the 2026-08-11 inbox pass: `Cancel` "overridden by
  nothing" (three nodes override it), "two unstable keys" (thirteen), the Promise registry being dead code (it
  is load-bearing for `SerializeAnimation`), and timer cancellation being cosmetic. **Read the code before
  trusting a task note.**

## Completed 2026-09-04 (earlier)

- [x] **OdinSerializer removed — replaced by a serializer we own.** The headline feature's engine, rebuilt and
  proven on a real player build. `Assets/Narramancer/Scripts/Serialization/`, ~1,000 LOC, 441 EditMode tests.
  Plan and full detail in [`docs/ODIN_REMOVAL_PLAN.md`](docs/ODIN_REMOVAL_PLAN.md).

  **Why:** a buyer who owned Odin Inspector hit a duplicate-assembly conflict on import, because both shipped
  OdinSerializer — a conversion blocker in exactly the segment most likely to pay. What it turned into was a
  correctness fix.

  **The bug it fixed.** Odin stored Unity references as `instanceID`s, handed out by a session-global counter
  in load order — a save recorded *"the 46,810th object loaded this session"* and nothing about the asset, so
  shipping a patch renumbered everything. Reproduced 2026-08-27: every reference shifted by exactly 44, and
  the save loaded with **no errors and no nulls**, resuming the story one node early. Silent corruption of the
  one feature the plugin is sold on. References are now keyed by `<asset GUID>:<local file id>`, a
  `res:Path/Name` Resources path, or a `ref:<key>` runtime registration.

  **Proven, not argued** *(WebGL release build, 2026-09-04)*:
  - The round trip through a full app restart — suspend mid-verb → save → close the tab → reopen → load →
    resume, on the right node.
  - **The patch test** — save, change the project's assets, rebuild, load that save in the new build. Resumes
    correctly. This is the check that failed silently before, and no EditMode test can stand in for it: every
    editor test runs in one session, where a session-local id looks exactly as good as a persistent one.

  *Things that turned out differently than planned, worth keeping:*
  - **The AOT risk was the big unknown and it evaporated.** A pure-reflection serializer with no `link.xml`,
    no `[Preserve]`, and managed stripping on Low survives IL2CPP. A second build smoke was planned to isolate
    whether Odin's AOT preprocessor had been helping — it never ran at all (Odin has no build hooks and its
    AOT utilities were never called), so that cycle was folded into the patch test's build.
  - **v2's `TypeResolver` was not ported.** It drags in a whole naming subsystem v1 has no other use for;
    `TypeNames` (~140 LOC) does the job behind two methods a vocabulary can slot in behind later.
  - **Two gaps the plan did not predict**, both found by writing the thing: Odin's policy serializes *every*
    field of a compiler-generated type regardless of visibility, which `Promise.doneCallbacks` depends on —
    missing it would have compiled, passed most tests, and broken every suspended verb. And NaN/infinity have
    no JSON representation; Odin wrote bare tokens that are not legal JSON.
  - **The test suite had been depending on `instanceID` identity.** Tests build nodes and nouns with
    `CreateInstance`, which yields objects that are not assets and have no persistent id at all. They now
    register with `RuntimeAssetRegistry`.
  - **The golden-file check in plan §5.2 was not executable as written** — the 40-save corpus comes from six
    other projects and its `$eref`s point at assets that do not exist here. It is a format inventory, not a
    fixture. Substituted: this repo's own two saves, from a live play session, which round-trip
    **byte-identically** under `Serialize(Deserialize(Serialize(x)))` and shrank 8,891 → 3,067 bytes and
    8,889 → 3,189.

  *Deliberately NOT done:* the Odin Inspector coexistence check — the actual commercial goal — still needs a
  copy of Odin Inspector and a scratch project. Left open in `tasks.md`, not blocking.

- [x] **Release runbook written** — [`docs/RELEASE_RUNBOOK.md`](docs/RELEASE_RUNBOOK.md). The three checks
  EditMode structurally cannot make (build smoke, patch test, Odin coexistence), what each failure mode looks
  like, and which kind of change triggers which.

- [x] **§2 resequenced into four milestones** *(2026-09-03)*. The old order ran `Continuation` and three
  smaller items *before* the serializer swap, which put 8–11 evenings of work on top of an unmeasured AOT
  question. Swap first, smoke immediately, batch the rest behind a second smoke. Vindicated: the swap took
  about a day and the smoke passed first try.

---

## Completed 2026-08-27

- [x] **`Continuation` design + characterization tests for the polling nodes.** `Tests.Editor` at **366
  passing**. Design in `docs/CONTINUATION_DESIGN.md`; `PollingNodeCharacterizationTests` pins current behaviour
  of the nodes the refactor will restructure, so it can be *verified* rather than hoped at.
  *Corrected a bad estimate first:* "1–2 evenings, one-liners capturing little more than which runner" came
  from sampling 3 of the sites. The real surface is **18 lambdas across 3 `SerializableAction` fields**, and 4
  nodes are timer-driven polling loops carrying a self-rescheduling local function plus shared mutable locals —
  ~4–6 evenings, some of it playtest-only.
  *Also reversed the sequencing:* this is **not** a port blocker. `System.Type` support is ~40 lines and
  closure reflection is free to a reflecting serializer, so it was trading ~5 evenings to save ~60 lines. Port
  first, this after — it stays pre-launch because it's only free while there are ~0 users.
  *Two more unstable-key bugs found while designing*, both the same family as the instanceID one:
  `Blackboard.UniqueKey` and `ParallelNode`'s sub-runner name are both built from `GetHashCode()`, so neither
  survives a save. `UniqueKey` is **load-bearing for this design** — it's where shared closure state would move
  — so shipping as-is would relocate the instability rather than remove it. Both are now prerequisites.

- [x] **Breadth coverage over every node type** — `Tests.Editor` goes from 60 to **359 passing**.
  `NodeTypeCoverageTests` is data-driven over `AssemblyUtilities.GetAllTypes<Node>()`, so all **149** concrete
  node types (147 shipped + 2 test doubles) are covered by two tests rather than 149 hand-written ones, and a
  node added tomorrow is covered the day it's written. Type coverage went 4% → 44%.
  *What it asserts:* every node type constructs through `graph.AddNode()`, gets its graph assigned, and builds
  its ports without throwing — the smoke test that catches a broken `Init()`/`UpdatePorts()`, which would
  otherwise only surface when an author happened to drop that node into a graph. And that a **reference** to
  every node type survives a save round trip.
  *Scope corrected while building it:* the task was originally written as "round-trip every node type", which
  would have proved almost nothing. **A node's own fields never enter a save** — verified against real saves,
  where node types appear only as `$eref` external references and never as an inlined `$type` (`bare
  PrintTextNode as $type: 0`). Node data lives in the `.asset` file, serialized by Unity, not Odin. So the test
  covers the *reference* path instead — which is exactly what the instanceID bug breaks and what the GUID-keyed
  resolver must preserve, making this the regression net for that swap.
  *Still not covered:* node **behaviour** (`Run`/`GetValue` logic) remains at zero. That is deliberate — 147
  behavioural suites is months of evenings for value no buyer perceives.

- [x] **Proved the `instanceID` reference bug (Landmine A).** Ran the editor half of
  `docs/INSTANCEID_EXPERIMENT.md`: saved, fully restarted Unity, saved again from the same story point, and
  compared the external reference lists. **All five instanceIDs differed.**
  *The shape of the result is the useful part:* every ID shifted by **exactly 44**, with identical internal
  spacing (gaps 2,2,4,2 in both saves). The assets loaded in the same relative order; the whole block just
  started 44 objects later. That is a **session-global sequential counter** — the save records "the 46,810th
  object loaded this session", never anything about the asset's own identity. So the reference breaks whenever
  *anything* changes how many objects load first, which is precisely what shipping a patch does.
  *The failure mode is the worst available:* loading save A in the new session gave **no error, no null, no
  wrong-typed object** — the story just resumed at the *previous* node. Nodes sit 2 apart in ID space and
  everything shifted by 44, so stale IDs landed on valid, correctly-typed, adjacent nodes. **Nothing detects
  this at runtime.** A player would see a save load cleanly and the story be subtly wrong.
  *Decision:* GUID-keyed references are **required**, not a nice-to-have. The Odin removal is a correctness fix,
  not a refactor.
  *Deliberately NOT done:* the full build-to-build Test 2. It would cost an hour to re-confirm a mechanism
  already demonstrated, and the decision it was meant to inform is settled. Supporting it: Unity documents
  instanceIDs as non-persistent, the 40-save corpus shows ~65 asset references per real save, and the
  originally-reported symptom ("saves break when the build or scene changed") is exactly this mechanism.

- [x] **`NodeRunner` harness + resume-after-load — the test-net gate is CLOSED.** `Tests.Editor` at **60
  passing** EditMode tests. `NodeRunnerTests` drives the runner by hand (`Update()` is normally pumped by
  `NarramancerSingleton`, but stepping it directly is what makes any of this testable outside play mode).
  *Covers execution* — queue order, going idle on drain, suspend halting before the next node, `Resume()`,
  `Resume(node)` inserting ahead of the queue, and `StopAndReset` abandoning queued work.
  *Covers the headline feature* — `SuspendedRunnerResumesAfterARoundTrip`: suspend mid-verb → serialize →
  deserialize → resume → the node queued behind the suspend point runs. Plus runner blackboards, run history
  (which drives the play-mode node highlight), and multiple independent runners.
  *Guarded against passing vacuously:* the flagship asserts the restored runner is a **different object**, and
  a negative control (`DrainedRunnerHasNothingToResumeIntoAfterARoundTrip`) shows a runner with a legitimately
  empty queue runs nothing — so the resume test can only pass if `queuedNodes` genuinely survived the boundary.
  **Everything marked *(needs the test net)* in `tasks.md` §2 is now unblocked.**

- [x] **Test net: save/load round-trip + domain ops** — `Tests.Editor` goes from 24 to **49 passing** EditMode
  tests, across `SaveLoadRoundTripTests` (new), `NounInstanceTests` (new) and `BlackboardTests`.
  *Extracted the serializer seam first:* `SaveLoadUtilities.SerializePayload`/`DeserializePayload` now wrap the
  two `SerializationUtility` calls, and `SerializeData`/`DeserializeData` call through them. Tests target the
  payload pair because `SerializeData` also captures a screenshot for the thumbnail and EditMode has no frame
  to capture — and it's the exact seam the Odin swap replaces, so the swap now touches one place.
  *Covered:* story blackboard, scene index, text logs, noun identity and per-noun blackboards, relationship
  integrity by UID **across the serialize boundary**, shared-reference preservation, asset refs through the
  external object list, and reconstruction of types that have no parameterless constructor.
  *Not yet covered:* the suspend/resume half of the round trip — needs a `NodeRunner` harness, tracked in
  `tasks.md` §2.

- [x] **Fixed `NounUID` comparing by reference instead of value** — found by the new tests, and it was
  load-bearing.
  `NounUID` is a class with an implicit `string` conversion but no `operator ==`. A user-defined conversion is
  not a *standard* implicit conversion, so predefined operator resolution never considers `string ==` and
  `==` fell back to **reference equality** — two UIDs with identical values compared as different.
  *Why it appeared to work:* `RelationshipInstance`'s constructor does `this.source = left.UID`, storing the
  very same object as `NounInstance.uid`. All eight `==` comparison sites therefore matched by accident of
  object identity — and survived saves only because OdinSerializer preserves shared references (`$iref`).
  **Relationship integrity silently depended on a serializer implementation detail**, which the Odin
  replacement would have broken with no test to catch it.
  *Also fixed by the same change:* `NarramancerSingleton.GetInstance(NounUID)` compares with `==`, so any
  caller passing a UID *string* got a new object via the implicit conversion, compared reference-unequal, and
  received **null every time**.
  Added `operator ==`/`!=` with ordinal value semantics, made `Equals` agree (including against a raw string),
  null-guarded `GetHashCode` and the implicit string conversion. Strictly more permissive than before — same
  object still compares equal — so no existing behavior regresses.

## Completed 2026-08-12

- [x] **Tier A bug-fix batch — 9 of 10 landed** (A10 deferred with reason, see `tasks.md` §1). All from the
  Pseudo World Gaia port-back survey; see `docs/UPSTREAM_PORT_SCOPE.md` for the original table. Compiles clean,
  24/24 EditMode tests still pass.
  - **A1 `VerbGraphInspector`** — the `#if ODIN_INSPECTOR` branch didn't compile: a typo'd
    `DuplciateNodeGraphField` and a cast to `NarramancerGraph`, a type that doesn't exist here (→ `VerbGraph`,
    matching the `#else` branch). Two lines. **Highest value-per-line in the survey:** it's dead code in this
    repo but the *only* branch an Odin Inspector owner compiles — i.e. exactly the buyer segment the §2 Odin
    removal exists to protect.
  - **A2 `StatInstance`** — the min/max clamp read the incoming parameter instead of the already-clamped field,
    so the second clamp recomputed from the unclamped input and silently discarded the first. **A stat with
    both a min and a max only honored the max.**
  - **A3 `AddRelationshipNode`** — `||` where `&&` was meant; null-dereferenced whenever exactly one of
    instance/relationship was set, i.e. precisely the case the guard existed to catch.
  - **A4 `NounInstancesQuery`** — `Equals`/`GetHashCode` null-dereferenced on an unset property array. Both now
    treat null and empty as the same thing ("no constraint") and **agree with each other**, which is required:
    this struct is a dictionary key in `NarramancerSingleton.queryInstancesTable`, so a hash/equals
    disagreement would corrupt lookups rather than just throw. *(Wrote the guard directly rather than porting
    the fork's, which had a typo — one branch compared `mustHaveProperties` against `mustNotHaveProperties`.)*
  - **A5 `GameObjectDistanceCheckNode`** — no null check before reading `.transform` on either GameObject.
  - **A6 `VerbGraphEditor.GetPortStyle`** — no guard for a null port or null `ValueType`; threw while a dynamic
    port was mid-rebuild, which the editor can repaint during.
  - **A7 `NarramancerGraphEditorUtilities`** — two `while` loops with no visited-set, so a **cyclic graph hung
    the editor** and grew the bucket unboundedly. Both now stop on a node already collected.
  - **A8 `NamedPrimitiveValueListDrawer` + `NounScriptableObjectListDrawer`** — both cached
    `Resources.Load<Texture2D>("d_winbtn_win_close@2x")`, which resolves to `null` because that's a built-in
    editor icon, not a Resources asset; the search-clear button rendered blank. Now
    `EditorGUIUtility.IconContent("winbtn_win_close")`.
  - **A9 `AssemblyUtilities` fuzzy-AQN regex** — matched only a *single-segment* namespace, so any type in a
    nested namespace failed the fallback entirely. **Verified after the fix:** `A.B.C.MyType` → `MyType`,
    `Deep.Nested.Name.Space.Thing` → `Thing`, single-segment and `UnityEngine.*` still fine, `GetType`
    round-trip intact.
    *This one was nearly skipped:* the survey flagged it as subsumed if the `TypeCache` work deleted the fuzzy
    fallback — but that deletion was deliberately **not** done (see 2026-08-11), so A9 was still live.

---

## Completed 2026-08-11

- [x] **Swapped the assembly scanner to `UnityEditor.TypeCache`** — `AssemblyUtilities.GetAllTypes(Type)` and
  `GetAllTypes<T>()` now use `TypeCache.GetTypesDerivedFrom` in the editor, with the old `AppDomain` scan kept
  as the runtime path (`TypeCache` is editor-only). Everything built on those — `GetAllNonObsoleteTypes`,
  `GetAllTypesInNamespace` — inherits the win.
  *Measured: **137ms → 0.05ms per call** (~2500x).* That 137ms was a visible hitch every time the node-search
  window opened.
  *Two semantic differences had to be corrected for, or results would silently change:*
  `GetTypesDerivedFrom` **excludes the queried type itself** (the old `IsAssignableFrom` included it, so the
  type is re-added when it's concrete), and it **returns abstract types** (the old scan filtered them out).
  *Verified rather than assumed:* diffed old vs new result sets across six probes — `XNode.Node` (147),
  `ScriptableObject` (1268), `AbstractIngredient` (5), `RunnableNode` (56), `NounScriptableObject` (1, the
  self-inclusion case), and `IInstancable` (interface, 3). **Zero missing, zero extra on every one.**
  *Deliberately NOT done:* the task also said to drop the fuzzy-AQN fallback in `GetType()`
  (`AssemblyUtilities.cs`, the `#if UNITY_EDITOR` block). Left in place — it silently recovers `SerializableType`
  /`SerializableMethod` fields whose scripts changed assemblies, so removing it is a behavior change that could
  break the sample scenes, and it is *not* what made the editor slow (it only runs after `Type.GetType` already
  failed). Re-evaluate once the test net covers those fields.

- [x] **Fixed the `FindObjectsOfType` deprecation warnings** — the `GameObjectExtensions` wrapper plus the
  direct sites in `PlayAudioNode`, `PlaySoundNode`, and `VerbGraphEditor` (×2), which now route through the
  wrapper. `Narramancer` and `Narramancer.Editor` compile warning-free.
  *The catch worth remembering:* on Unity 6, **`FindObjectsSortMode` is itself deprecated**, along with every
  overload that accepts one — so the fix everyone reaches for (`FindObjectsByType<T>(FindObjectsSortMode.None)`,
  which is what the plan doc specified) is *also* deprecated here. Unity's message: *"InstanceID will be
  replaced in the future with EntityId and previous sort order cannot be maintained."* The correct Unity 6 call
  is the no-sort-mode overload. Guarded `UNITY_6000_0_OR_NEWER` / `UNITY_2021_3_OR_NEWER` / legacy so older
  Unity support is unaffected.
  *Consequence to know:* results are now **unordered**. The `FirstOrDefault` callers (`ChoicePrinter`,
  `PrintTextNode`, `SerializableVariableReference`) are only correct because they either filter to a unique
  match or genuinely accept any one — don't add a caller that assumes "the first one."

- [x] **Started the test net: `Blackboard` typed set/get/remove** — `Tests.Editor` went from 3 tests to **24,
  all passing**. Covers generic `Set`/`Get` across int/float/bool/string/enum, missing-key defaults, explicit
  defaults, the `TryGet*` pair, `Remove`/`GetAndRemove`/`Clear`, `Increment`/`Decrement`, `IntKeys`, `Copy`
  independence, and JSON round-trips.
  *Two behaviors worth knowing, now pinned by tests:* missing keys return the type default (`0`, `false`, `""`)
  rather than throwing, and **each type has its own backing dictionary**, so the same key can hold an int, a
  string and a bool simultaneously and `Remove<int>` leaves the others untouched.

- [x] **Decided: relicense to PolyForm Noncommercial 1.0.0, going forward only.** Free to read, fork, modify,
  and use noncommercially; **commercial use requires an Asset Store purchase**. Dual-licensing model.
  *Why PolyForm Noncommercial specifically:* it's the off-the-shelf license that says exactly this, in plain
  language, professionally drafted — no bespoke legal text to get wrong. Alternatives were considered and
  rejected: PolyForm Free Trial (32-day evaluation only — too restrictive for "evaluate freely"), and BSL 1.1
  (adds a change-date conversion this doesn't need).
  *Prior versions stay Apache-2.0, permanently* — that grant is irrevocable and no attempt is made to withdraw
  it. Anyone who obtained the code before the relicense keeps those rights for those versions. The old text is
  preserved at `LICENSE-APACHE-2.0.txt`; the change is anchored to "the commit that introduced the new LICENSE
  file," so it stays unambiguous no matter what lands afterward.
  *Two things verified before doing it:* the repo has a **single copyright holder** (287 commits from Brian,
  plus one AI-authored commit made at his direction), so there's no contributor consent problem; and all four
  vendored dependencies are permissive (MIT / Apache-2.0), so they're fine to redistribute commercially but
  **are not ours to relicense** — they keep their own terms, carved out explicitly in `LICENSE`.
  *The trap to remember:* PolyForm Noncommercial is **not OSI-approved open source**. Say "source-available."
  Getting this wrong in the README or an r/Unity3D post turns a selling point into an argument.
  *Effect on the abandonment pitch* (the load-bearing objection for a save system): mostly unchanged. Buyers
  still get perpetual rights plus full source access; what they lose is the theoretical ability of a
  *non-purchaser* to fork and ship commercially — which is the point.
  *Shipped* in `LICENSE` (scope preamble + verbatim upstream PolyForm text, verified byte-identical to the
  canonical `.txt` from polyformproject.org), `LICENSE-APACHE-2.0.txt` (previous grant preserved, verified
  identical to the old `LICENSE`), and `LICENSING.md` (plain-language summary). Reviewed and approved by Brian
  before the commit.
  *Still open, low urgency:* set up a CLA/DCO before accepting any external pull request — `tasks.md` §1.

- [x] **Decided: Core extraction (B5) stays gated behind the 30-day marketing measurement.** It's the item most
  likely to eat a month of evenings for benefits no buyer can perceive, and it's the gateway to scope-drifting
  back into the paused v2 rewrite. If the measurement comes back flat, skip it entirely rather than
  "improving" a product the market didn't want. Parked in `tasks.md` under Future / Nice to Have.

- [x] **Decided: breaking engineering work ships _before_ the relaunch, not during the measurement window.**
  Reverses the sequencing in the original roadmap, which launched first and used the quiet 30-day window for
  the surgery.
  *Reasoning:* the save-format changes (Odin removal, Saveable refactor) are free only while there are ~0
  users. That window is **guaranteed now and only probabilistic after a launch** — and if the marketing
  succeeds, which is the entire point, it closes. A save system that breaks saves in a follow-up release would
  destroy precisely the trust ("will this still exist in a year?") that the repositioned listing is built to
  establish.
  *Cost accepted:* launch slips ~12–17 evenings, against a validation thesis that says learn cheaply and early.
  Hedged by running the exposure-neutral diagnostics (baseline, Description-field check) immediately, in
  parallel — they cost nothing and don't make the product more purchasable.
  *Follow-on decisions this settled:* the Saveable refactor is **in scope and pre-launch** (it was an open
  question); the demo video is **in the launch bundle** and gets recorded **after** the refactor so inspector
  footage shows the single `Saveable` component and doesn't need re-shooting; and there is now **one release,
  1.0.0**, not a 1.0.0 launch followed by a 1.1.0 — which also means the 30-day measurement window is finally
  a genuinely quiet one.

- [x] **Task Z — save/load verified in built players on Unity 6.** Both builds round-tripped: run a verb to a
  suspend point → save → **fully close the app** → reopen → load → resumed where it left off.
  - **Mono standalone (Linux):** passed.
  - **WebGL:** passed, including across a page reload.
  - *Why WebGL is the one that mattered:* it is **IL2CPP-only**, so it exercised the AOT codegen path
    (`AOTSupportScanner` + the build preprocessor) that this whole task existed to probe — the historical
    OdinSerializer failure mode, which editor play mode and the Mono build both structurally cannot catch. It
    also ran under managed stripping (`managedStrippingLevel: 1`), the other classic failure mode for
    reflection-based serialization. A desktop IL2CPP build would have been *weaker* evidence than this, so it
    was **deliberately not run** — don't re-add it as a blocking task.
  - **What this unblocks:** the marketing track. The headline claim ("save and restore a running graph at any
    point") is now verified on Unity 6 in shipped players, on the two backends that matter, which is what the
    listing rewrite and demo video rest on.
  - **Carry forward:** re-run this smoke before every release — it's the only check that covers AOT + stripping,
    and no EditMode test can replace it.

## Completed 2026-08-11 — marked done during the task-system consolidation

These were still listed as open to-dos across the planning docs, but were verified
already landed in the working tree when `tasks.md` was seeded.

- [x] **Upgrade to Unity 6** — `ProjectSettings/ProjectVersion.txt` is on `6000.4.12f1` (commit `89b173b`);
  editor opens on it and the console is clean, 0 errors and 0 warnings. The planning docs all still describe
  this as the "BLOCKING GATE — not started," so **the docs were stale, not the code**.
  *Gotcha worth keeping:* a clean editor compile is **not** proof the plugin works on 6. Odin's AOT codegen,
  `AOTSupportScanner`, and `NarramancerSingleton.OnPreprocessBuild` only run during a real player build — which
  is why Task Z existed. Task Z has since passed (entry above), but the principle still holds for future work:
  editor-green means nothing about a shipped build.

- [x] **WebGL save persistence fix (`FS.syncfs` → IndexedDB)** — all three pieces landed:
  `Assets/Narramancer/Scripts/Plugins/WebGL/SaveFileSync.jslib`,
  `Assets/Narramancer/Scripts/Utilities/SaveFileSync.cs`, and the flush call inside
  `SaveLoadUtilities.WriteSaveData` (line 40). Putting the flush inside `WriteSaveData` covers every caller
  automatically. Root-cause writeup kept at `docs/WEBGL_SAVE_PERSISTENCE_FIX.md`.
  **Verified end-to-end in a real WebGL build the same day** (see the Task Z entry above) — a save survives a
  page reload, which it did not before this fix. Note for later: *if a delete-save path is ever added, call
  `SaveFileSync.Flush()` after the `File.Delete` too*, or the deleted save reappears on the next page load.

- [x] **Consolidate the planning docs** — moved `REBUILD_PLAN.md`, `SAVEABLE_REFACTOR_HANDOFF.md`,
  `V1_IMPROVEMENT_PLAN.md`, `WEBGL_SAVE_PERSISTENCE_FIX.md` and the validation handoff into `docs/`, and wrote
  `docs/V1_ROADMAP.md` folding the marketing and engineering tracks into one sequence.
  *Deliberately not done:* the older docs were **not** deleted or rewritten. `V1_IMPROVEMENT_PLAN.md` is
  superseded on *sequencing* only — it's still the reference for why items #1–#5 matter. `REBUILD_PLAN.md`
  describes v2, which stays paused at phase 2 of ~8; it's kept as the R&D kit the v1 work harvests from
  (`NarraSerializer`, the Core-split pattern, the `TypeCache` spec), not as active work.

- [x] **Adopt the three-file task system** — `tasks.md` / `inbox.md` / `done.md` at the repo root, per
  `docs/task-system-handoff.md`, plus a `CLAUDE.md` pointing sessions at them.
  *Split to preserve:* `docs/V1_ROADMAP.md` owns the **strategic** view (why this order, the tradeoffs, how the
  two tracks interlock); `tasks.md` owns the **tactical** one (what's next, in order). Cross-linked both ways so
  they don't drift into two competing lists.
