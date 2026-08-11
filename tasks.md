# Tasks

**This is the canonical to-do list for Narramancer.** If you're a new session
looking for "what's next," start here. Pick the top unfinished item in the
highest active section unless the user says otherwise.

---

## How this system works

Three markdown files at the repo root work together:

| File | Purpose |
|---|---|
| **`tasks.md`** (this file) | The live roadmap + next unfinished tasks. The source of truth for active work. |
| **`done.md`** | Archive. Completed tasks and whole finished phases get **moved** here (not deleted) so this file stays short. |
| **`inbox.md`** | A drop box for new tasks. Parallel sessions append here instead of editing `tasks.md`, which avoids merge conflicts. Periodically folded into `tasks.md` and emptied. |

The **strategic** view — why this order, what the tradeoffs are, how the
marketing and engineering tracks interlock — lives in
[`docs/V1_ROADMAP.md`](docs/V1_ROADMAP.md). This file owns the **tactical** view:
what's next, in order. Read the roadmap once; work from here.

### The golden rule

**Update the task docs in the _same commit_ as the work they describe.**
When you add, edit, complete, or move a task, the change to `tasks.md` /
`done.md` / `inbox.md` rides along in the same commit as the code. That way the
git history and the task list never disagree, and the next session can trust
what these files say.

### Task format

```markdown
- [ ] **Short title** — one-line description. Optional pointer to a doc or file.
```

- Use `- [ ]` for open, `- [x]` for done, `[~]` for "shipped with a known sliver."
- **Bold** the short title so the list scans quickly.
- Keep each task to a line or two. Deep design detail belongs in a
  `docs/*-handoff.md` doc that the task links to, not inline here.

### Working a task

1. Pick the top open item in the highest active section (or whatever the user asked for).
2. Do the work. If you learn something the next session needs, jot it on the task line or in the doc it links to.
3. When it's finished, **move** the line to `done.md` — don't just check it off and leave it here. Small, closely-related items can be batched.
4. Commit the code **and** the task-doc edits together.

### Adding a task

- **Working in this session, on the main line of work?** Add it directly to the right section below.
- **A parallel/background session, or just capturing a stray idea?** Append it to [`inbox.md`](inbox.md) instead.

### Folding the inbox

When `inbox.md` has accumulated items, sort each one into the right section
here, tag it `(from inbox YYYY-MM-DD)`, then clear it back to its empty
template. Do this as its own small commit — see `inbox.md` for the procedure.

---

## The ordering decision (2026-08-11)

**Breaking engineering work lands *before* the relaunch, not after it.**

The earlier plan launched first and did the breaking work during the 30-day
measurement window. Reversed deliberately: the save-format changes (Odin removal,
Saveable refactor) are free only while there are ~0 users, and that window is
guaranteed *now* but only probabilistic after a launch. If the marketing works —
which is the entire point — the window slams shut, and a save system that breaks
saves in a follow-up release would destroy the exact trust the marketing is
built to establish.

**The cost, stated honestly:** launch slips by roughly 12–17 evenings, and the
validation handoff's whole thesis is to learn cheaply and early. Mitigated by
running the free, exposure-neutral diagnostics (§1) *now*, in parallel — they
cost nothing and don't make the product any more purchasable.

**Consequence:** there is now one release, not two. Everything ships as **1.0.0**.

---

## Now / In progress

- [ ] **Start the test net** (§2) — it's the precondition for both breaking changes, so it's the real
  critical path. Run the §1 diagnostics alongside it; they're a different kind of evening.

---

## Decisions needed

_None open. Both were resolved 2026-08-11 — see [`done.md`](done.md)._

---

## 1. Pre-work — do now, in parallel

Free, and **exposure-neutral**: none of this makes the product more discoverable or purchasable, so it doesn't
compromise the "land the breaking changes before anyone can buy" ordering. The baseline in particular has to be
recorded before anything changes anyway.

- [ ] **Record the views/day baseline** — publisher dashboard, before any change. Baseline is ~3–4/day.
  Confirm the referrer breakdown while you're in there. **Do this first** — once the listing changes, the
  pre-change number is unrecoverable.
- [ ] **Verify the listing's Description / Technical Details are populated at all** — they rendered empty on
  fetch. Check in a logged-out browser. Pure diagnostic, changes nothing. If blank, that alone plausibly
  explains 22 favorites against ~0 sales, and it means the listing has never had a fair test — which *raises*
  the value of launching from a hardened build rather than re-running a broken launch.
- [ ] **Review and commit the license change** — drafted 2026-08-11, not yet reviewed or committed.
  [`LICENSE`](LICENSE) is now PolyForm Noncommercial 1.0.0 (canonical text) with a scope preamble;
  [`LICENSE-APACHE-2.0.txt`](LICENSE-APACHE-2.0.txt) preserves the prior grant; [`LICENSING.md`](LICENSING.md)
  is the plain-language summary. Read both before committing — this is the one item here with legal weight, and
  it's worth an hour of your own eyes even though the license text itself is unmodified boilerplate.
  - **Commit it as its own commit**, so the effective date is unambiguous (the preamble anchors the change to
    "the commit that introduced this file").
  - **Never call it "open source"** in the listing, README, narramancer.com, or any forum post — PolyForm
    Noncommercial is not OSI-approved. Say **"source-available."** See `LICENSING.md`.
  - Set up a CLA/DCO *before* accepting any external pull request, or future licensing changes get blocked.
- [ ] **Fix `FindObjectsOfType` deprecation warnings** — fix the wrapper once at
  `Assets/Narramancer/Scripts/Extensions/GameObjectExtensions.cs:13`
  (`→ FindObjectsByType<T>(FindObjectsSortMode.None)`), which covers everything routed through it. Then the
  direct sites: `PlayAudioNode`, `PlaySoundNode`, `VerbGraphEditor` (×2), `SerializableVariableReference`.
  **Leave `Resources.FindObjectsOfTypeAll` alone** — not deprecated. Good warm-up task.
- [ ] **Swap the assembly scanner to `TypeCache`** — replace the `AppDomain` scan at
  `Assets/Narramancer/Scripts/Utilities/AssemblyUtilities.cs:16` with `UnityEditor.TypeCache`. Immediate editor
  responsiveness win with 156 node types being scanned. Drop the fuzzy-AQN fallback (lines 285–299) — v2 already
  retired it. Note `TypeCache` is editor-only, so this is a `#if UNITY_EDITOR` fast path, not a wholesale
  replacement.

---

## 2. Breaking changes — land these before anyone can buy

**This is now the critical path.** With ~0 users there is no save-format migration burden; that is the whole
reason this section moved ahead of the launch. Nothing here is safe to start before the test net exists.

- [ ] **Build the test net** — precondition for everything below it. Currently one 50-line file
  (`Assets/Test Suite/Editor/BlackboardTests.cs`). Build on the existing `Tests.Editor` asmdef; EditMode is
  enough. Priority order:
  - [ ] Save → load round-trip: author a story, run a verb to a suspend point, `PrepareStoryForSave()` →
    `SerializeData` → `DeserializeData` → `LoadStory`; assert identical state and that the runner resumes.
    **The headline feature currently has zero coverage.**
  - [ ] `Blackboard` typed set/get/remove (extend the existing file)
  - [ ] Domain ops: `NounInstance` properties/stats/relationships, bidirectional relationship integrity by UID
  - [ ] `NodeRunner` suspend/resume
  - [ ] Write the release runbook: the Task Z build smoke (IL2CPP/WebGL round-trip) is the one check EditMode
    structurally cannot catch — record it as a required pre-release step
- [ ] **Remove OdinSerializer** — a buyer who owns Odin Inspector hits a duplicate-assembly conflict because
  both ship OdinSerializer. That's a conversion blocker, not cleanup. Scope is far smaller than the 186
  vendored files suggest: **exactly 2 call sites**, both JSON-only, both in
  `Assets/Narramancer/Scripts/Utilities/SaveLoadUtilities.cs` (lines 56 and 82). Port `NarraSerializer` from
  v2 behind those two calls, then delete the `Narramancer.OdinSerializer` assembly. Ship a `schemaVersion` in
  the envelope from day one; fail with a clear "incompatible save" message rather than a crash.
  *(needs the test net)*
- [ ] **Saveable refactor — one component + drivers** — see
  [`docs/SAVEABLE_REFACTOR_HANDOFF.md`](docs/SAVEABLE_REFACTOR_HANDOFF.md) for the full design. Do it adjacent
  to the Odin removal; both touch the save format.
  The real reason to do it: the current key is `$"{field} {transform.FullPath()}[{componentIndex}]"`
  (`SerializableMonoBehaviour.cs:22`), so renaming an object, reparenting it, reordering components, or two
  same-named siblings **silently breaks the save** — a latent correctness bug in the headline feature, and the
  single strongest reason this section goes before the launch rather than after.
  Keep driver `StateType`s as simple serializer-friendly structs so they survive the serializer swap.
  *(needs the test net)*
  - [ ] `Saveable` + stable GUID (mint at author time; duplicate-paste detection — copy Unity's open-source
    `GuidComponent` approach; prefab assets carry no guid)
  - [ ] `IComponentSaver` registry + the 9 built-in drivers (port the `Serialize*` table)
  - [ ] `[Save]` behavior-field scan (rename of `[SerializeMonoBehaviourField]`), incl. `NodeRunner`/`Promise`
    side-table routing under the guid key
  - [ ] Migrate `RunActionVerbMonoBehaviour` + `NarramancerScene` to the guid model
  - [ ] Spawned-object guid reassignment in `SerializableSpawner`
  - [ ] Update the 4 sample scenes; EditMode tests per driver
- [ ] **Re-run the Task Z build smoke** — **required, not optional.** Both refactors change serialization
  fundamentally, and the original Task Z pass no longer covers the new code. Repeat the full round-trip
  (run → suspend → save → close the app → reopen → load → resume) on a **WebGL build** at minimum, since it's
  IL2CPP-only and therefore covers AOT codegen + managed stripping in one shot. See `done.md` for why WebGL is
  the decisive build.
- [ ] **Multiple xNode editor windows** — non-breaking and independent, so it gates nothing. Room to ride along
  before the launch and give the relaunch a genuinely new feature to tout, but **don't let it delay §3.**
  ~2–3 evenings: four specific pieces of global state to make per-window, plus one known limitation to accept
  rather than fix. **Read [`docs/V1_ROADMAP.md`](docs/V1_ROADMAP.md) §4/B6 before starting** — it has the
  file:line breakdown.

---

## 3. Launch — the relaunch bundle (ships as 1.0.0)

Everything here goes out together, on a build that already has the new serializer and the `Saveable` model.
**Nothing in this section starts until §2 is done and the build smoke has been re-run.**

- [ ] **Record the demo video** — 60–90s, no narration: run a graph → pause mid-execution → save → quit the
  editor entirely → reopen → load → resume exactly where it was. **Record it after the Saveable refactor**, so
  any inspector footage shows the single `Saveable` component rather than the old `Serialize*` zoo and doesn't
  need re-shooting. Use on the listing, README, narramancer.com, and as the r/Unity3D post.
- [ ] **Republish as 1.0.0 with Unity 6 compatibility** — drop the 0.x (loudest "unfinished" signal), declare
  6000.x, then verify the live compatibility table actually shows Unity 6. Success check: the asset appears
  when browsing Visual Scripting filtered to 6000.x.
- [ ] **Rewrite the listing around save/load** — lead with the pain, not the mechanism. "Node graph tool" is a
  commodity with a free first-party incumbent; "graph tool with save/restore built into the execution model"
  is close to a category of one. Title → `Narramancer — Node Graphs with Built-In Save/Load`.
- [ ] **Price → $39** — expect this to *raise* conversions. No launch discount; $7.99 reads as abandonware.
- [ ] **Replace the keyword set** — drop unwinnable generics (`logic`, `Tool`, `Node`, `flow`, `visual`,
  `Graph`) for high-intent terms: `save system`, `save and load`, `game state`, `serialization`, `checkpoint`,
  `save anywhere`, `visual novel`, `dialogue`. Also confirm which category is primary.
- [ ] **Rewrite README as a landing page** — demo GIF at top, one paragraph, link to the listing. Currently
  it's feature documentation. Repos rank in Google for queries the Asset Store page never will.
- [ ] **Content-level secret scan of git history** — a filename scan across all history came back clean (no
  `.env`, `.pem`, `*key*`, `*token*`, `*credential*` ever added). Run `gitleaks` or `trufflehog` for a content
  pass before leaning on the repo as a channel.
- [ ] **narramancer.com: move the Naninovel recommendation** — it currently talks visual-novel devs out of the
  product inside the pitch paragraph, at the moment of decision. Keep the honesty; relocate to a comparison
  page further down the funnel.
- [ ] **narramancer.com: hero leads with save/load** — match the listing. Store link above the fold. Docs
  cover Unity 6 setup.

---

## 4. After launch

- [ ] **Measure** — 30 days after launch, **no changes in between**, compare to the §1 baseline. This window is
  now genuinely quiet, since the engineering that used to fill it has already shipped. If something must go out
  mid-window, restart the 30 days.
  - Views up → compatibility/positioning was the bottleneck. Proceed to a keyword pass and wider promotion.
  - Flat → discovery problem persists. Diagnose before building anything more.
  - Up but no sales → conversion problem. Reviews and a free/lite tier are the next levers.

---

## Systems & features (design docs / handoffs)

Larger builds that each have their own design doc — read the linked handoff before starting.

- [`docs/V1_ROADMAP.md`](docs/V1_ROADMAP.md) — the strategic view: verified project state, how the marketing
  and engineering tracks interlock, sequencing, risks. **Read this first.**
- [`docs/narramancer-v1-handoff.md`](docs/narramancer-v1-handoff.md) — validation/marketing handoff:
  competitive landscape, the diagnosis, the measurement plan.
- [`docs/SAVEABLE_REFACTOR_HANDOFF.md`](docs/SAVEABLE_REFACTOR_HANDOFF.md) — full design for the single
  `Saveable` component + driver registry + GUID identity.
- [`docs/V1_IMPROVEMENT_PLAN.md`](docs/V1_IMPROVEMENT_PLAN.md) — the original 5-item engineering plan.
  Superseded on sequencing by `V1_ROADMAP.md`; still the reference for items #1–#5 rationale.
- [`docs/WEBGL_SAVE_PERSISTENCE_FIX.md`](docs/WEBGL_SAVE_PERSISTENCE_FIX.md) — the `FS.syncfs` fix. Landed;
  kept for the root-cause explanation.
- [`docs/REBUILD_PLAN.md`](docs/REBUILD_PLAN.md) — **v2, paused at phase 2 of ~8.** Not active work. Referenced
  only as the R&D kit the v1 items harvest from (`NarraSerializer`, the Core-split pattern, `TypeCache` spec).
- [`docs/task-system-handoff.md`](docs/task-system-handoff.md) — how this three-file system works.

---

## Future / Nice to Have

- [ ] **Extract `Narramancer.Core`** — a `noEngineReferences` netstandard2.1 assembly for the serializer,
  domain model, and utilities, with a `dotnet/` mirror for fast Unity-free CI. **Stops at the xNode boundary**
  — nodes derive from xNode's `Node : ScriptableObject` and cannot move; pushing past that boundary *is* the v2
  rewrite. Genuine "harden for the long haul" value, but no buyer perceives it. **Skip entirely if the 30-day
  measurement comes back flat.**
- [ ] **Triage the 28 in-code `// TODO` comments** — mostly node-level polish and editor papercuts (predicate
  options on `OfferObjectsAsChoicesNode`, name-input dialogs in `ActionVerbListDrawer`, stat handling without
  min/max in `StatInstance`). Worth one pass to promote the few that matter and delete the rest, rather than
  seeding 28 lines here.
- [ ] **Rename the publisher from "Professional Bad Guys"** — signals nothing relevant to someone evaluating a
  save system. Low priority.

---

_See [`done.md`](done.md) for the archive of completed work._
