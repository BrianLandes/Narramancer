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

## Completed 2026-08-27

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
