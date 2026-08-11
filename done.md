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
