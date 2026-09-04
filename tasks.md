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

- [ ] **M2 — execution-model correctness.** M1 is done: the serializer is swapped, Odin is deleted, and both
  the build smoke and the patch test passed on a real WebGL player *(2026-09-04)*. Next up is the sub-runner
  tree and `RunnableNode.Cancel` — a live leak today — carrying the two `GetHashCode()` unstable-key fixes
  with it. See §2 M2 below.

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
- [ ] **Set up a CLA or DCO before accepting any external pull request** — not urgent (no external PRs yet), but
  contributions accepted without one land under terms that can block a future licensing change. Cheap now,
  expensive to retrofit. See [`LICENSING.md`](LICENSING.md).
- [ ] **A10 — log a null target object in `AbstractDynamicMethodValueNode`** — the one Tier A item left; the
  other nine landed 2026-08-12. Deliberately deferred rather than done: a null target is **legitimate** for
  `CallStaticMethodRunnableNode` (static calls), so an unconditional log spams the console on a correct path.
  Doing it properly means exposing staticness on `SerializableMethod`, whose `methodInfo` is `[NonSerialized]`
  and lazily resolved — more than the 1–10 lines the rest of Tier A cost, for a purely cosmetic gain. Fold it
  into whatever next touches `SerializableMethod`. *(from inbox 2026-08-11 — Tier A10)*

---

## 2. Breaking changes — land these before anyone can buy

**This is the critical path.** With ~0 users there is no save-format migration burden; that is the whole
reason this section moved ahead of the launch. The test net that used to gate this work **exists** — 438
EditMode tests, run with `run_tests` (EditMode, assembly `Tests.Editor`) — so nothing here is blocked on it
any more.

### Why this order (resequenced 2026-09-03)

The old order ran the `Continuation` record and three smaller items *before* the serializer swap. That was
written before the serializer existed and is now backwards, for one reason:

**The build smoke is the only check that cannot be automated, and the riskiest unknown sits behind it.** The
replacement serializer is pure reflection, there is no `link.xml` and no `[Preserve]` anywhere in the project,
WebGL managed stripping is Low, and deleting Odin also deletes its build-time AOT preprocessor. Whether that
survives IL2CPP is **completely unmeasured**, and no EditMode test can measure it. Following the old order
would put 8–11 evenings of `Continuation` and execution-model work on top of that unknown before finding out.

So: **swap first and smoke it immediately**, then batch the remaining format changes behind a second smoke.
The principle is to batch format-breaking work behind the manual check — but not the first piece, where the
unknown is concentrated and a failure needs to be isolated.

- [~] **The test net is up — the gate on the refactors below is CLOSED.** `Tests.Editor` is at **438 passing**
  EditMode tests; run with `run_tests` (EditMode, assembly `Tests.Editor`). Everything in this section that
  said *(needs the test net)* is now unblocked.
  - [x] Save → load round-trip, **including resume after load** *(done 2026-08-27)* — `SaveLoadRoundTripTests`
    covers the data path; `NodeRunnerTests.SuspendedRunnerResumesAfterARoundTrip` covers the headline feature:
    suspend mid-verb → serialize → deserialize → resume → the queued node runs.
  - [x] `Blackboard` typed set/get/remove *(done 2026-08-11)*
  - [x] Domain ops: `NounInstance` properties/stats/relationships, relationship integrity by UID
    *(done 2026-08-27; `NounInstanceTests` — found and fixed a real `NounUID` equality bug, see `done.md`)*
  - [x] `NodeRunner` suspend/resume *(done 2026-08-27; `NodeRunnerTests`)*
  - [x] Write the release runbook *(done 2026-09-03)* — [`docs/RELEASE_RUNBOOK.md`](docs/RELEASE_RUNBOOK.md):
    the build smoke, the patch test, the Odin Inspector coexistence check, and which change triggers which.

---

### M1 — Make the swap real ✅ *(done 2026-09-04)*

The replacement serializer is the serializer, OdinSerializer is deleted, and **both** build checks passed on a
real WebGL player: the round trip through a full app restart, and the patch test — save, change the assets,
rebuild, load — which is the one that proves a saved reference survives a renumbering. Full write-up in
[`done.md`](done.md).

- [x] **Remove OdinSerializer — done, archived to [`done.md`](done.md).** Phases 2–5 all landed; the WebGL
  build smoke and the patch test both passed on 2026-09-04.
- [ ] **Odin Inspector coexistence check** — install paid Odin Inspector alongside the plugin in a scratch
  project and confirm there is no duplicate-assembly error on import. This is the *commercial* goal the whole
  removal was for, so it is worth verifying directly rather than assuming. **Not blocking M2** — it needs a
  copy of Odin Inspector and can happen any time before §3 ships.
  Procedure: [`docs/RELEASE_RUNBOOK.md`](docs/RELEASE_RUNBOOK.md) §3.

---

### M2 — Execution-model correctness *(~3–4 evenings)*

Independent of the serializer, and worth doing before `Continuation`: it restructures the sub-runner nodes
once instead of twice.

- [ ] **Sub-runner tree + node cancellation** — `NodeRunner` gains a serialized `List<NodeRunner> subRunners`;
  `StopAndReset()` recurses into them and releases each; the nodes that spawn sub-runners register and
  deregister them.
  ⚠️ **The premise below was stale — corrected 2026-09-04.** `RunnableNode.Cancel` is *not* "overridden by
  nothing": `ParallelNode`, `ChoosePrioritizedBranchNode` and `WaitUntilNode` all override it. The real state
  of things is worse in one way and smaller in another:
  - Both existing sub-runner `Cancel`s work by **reconstructing the sub-runner's name from hash codes** and
    looking it up. After a save/load the name no longer matches: `ChoosePrioritizedBranchNode` null-checks and
    so silently fails to cancel (leak), `ParallelNode` does not and **throws a `NullReferenceException`**.
  - Only `RunActionVerbForDurationNode` and `RunActionVerbWhileConditionIsTrueNode` have no `Cancel` at all,
    plus `WaitNode`, which leaks its timer.
  - `RunActionVerbForDurationNode` names its sub-runner `$"{name} - {graph.name}"` — stable, but **not
    unique**, and `CreateNodeRunner` uses `Dictionary.Add`, so two concurrent instances of that node throw.
    (An earlier note called this naming "the right thing"; it is stable but collides.)
  Parent ownership fixes all of these at once, because the name stops being how a sub-runner is found.
  **Do this before `Continuation`** — 3 of the 14 `Promise` sites and **2 of the 4 hard polling nodes** are
  about sub-runner ownership, so doing it after would restructure them twice. A parent-owned tree may also
  make the `ReleaseRunner` continuation kind unnecessary.
  *(from inbox 2026-08-11 — Tier B1)*
  - [x] **`Blackboard.UniqueKey` no longer keys on the instanceID** *(2026-09-04)*. Bigger than the two sites
    the task claimed: **13 call sites across 9 node types** — loop indices, spawned objects, timers, chosen
    elements. And `UnityEngine.Object.GetHashCode()` **is** `GetInstanceID()` (verified, not assumed), so this
    was the *same* bug as the saved references, hiding in blackboard keys: every one of those changed meaning
    when the build changed, and the state read back as a default. Now keyed by durable id through
    `Serialization.PersistentId`, which shares the save system's resolver so keys and references cannot
    disagree. An owner with no durable id still works for the session but logs a warning saying it will not
    survive a save.
  - [ ] **`ParallelNode`'s sub-runner name — folded into the tree work above, not a separate fix.** It builds
    the name from three `GetHashCode()`s, then *reconstructs* it in `Cancel` to look the runner up — and
    `NodeRunner` is a plain C# object, so its hash changes every session. After a round trip the lookup misses
    and `Cancel` calls `StopAndReset()` on the null it got back. Parent-owned sub-runners delete the
    reconstruction entirely, so the name stops being load-bearing rather than being repaired.
- [ ] **Finish the Promise registry** — `StoryInstance.promises` (`StringPromiseDictionary`) exists with a
  public accessor and **nothing in the repo reads or writes it**. Add
  `NarramancerSingleton.MakePromise()/BreakPromise()/UpdatePromises()` driven from `Update()`, plus
  `Promise.WithUpdate(Action)`, `Promise.removeOnResolve`, and `Promise.DefaultDone`. Worth doing for the
  dead-code cleanup alone; it also puts live data into a serialized dictionary that currently ships empty, so
  it belongs here rather than after. Pair it with the sub-runner work above.
  *(from inbox 2026-08-11 — Tier B2)*

**Riders — no dependencies, do them whenever there is a gap:**

- [ ] **Fix `InequalityNode` comparison semantics** — `Comparison.GreaterThan` currently evaluates `>=` and
  `LessThan` evaluates `<=`. Adding `GreaterThanOrEqualTo`/`LessThanOrEqualTo` at indices 1–2 fixes the
  semantics *and* happens to be migration-safe: old index 1 (`GreaterThan`, behaving as `>=`) maps to new
  index 1 (`GreaterThanOrEqualTo`, `>=`), same for index 2. Existing authored graphs keep their exact behavior
  and gain correct labels. Rare freebie. Graph-enum change, not save-format. *(from inbox 2026-08-11 — Tier B3)*
- [ ] **Regression tests for the Tier A fixes** — nine real bugs were fixed on 2026-08-12 with **zero tests**,
  so nothing stops them coming back. Worth covering the pure-logic ones: `StatInstance` min+max clamp (a stat
  with both only honored the max), `NounInstancesQuery` null/empty equality *and* hash agreement (it's a
  dictionary key), the `AssemblyUtilities` nested-namespace AQN regex, `AddRelationshipNode`'s null guard, and
  `InequalityNode`'s comparison semantics once that lands. Skip the editor-UI ones (A6 port style, A8 icon) —
  not worth a harness. ~1 evening.

---

### M3 — Replace the serialized closures *(~4–6 evenings, smaller after M2)*

- [ ] **Replace serialized closures with a `Continuation` record** — full design in
  [`docs/CONTINUATION_DESIGN.md`](docs/CONTINUATION_DESIGN.md). **Sequenced after the serializer swap**, not
  before: the swap doesn't need it (`System.Type` support and closure reflection both landed in phase 2, and
  both are cheap). Real scope is **18 lambda sites across 3 storage mechanisms**, of which the choice-callback
  path needs manual playtesting. Once it lands the serializer can drop `$systemType` and the
  compiler-generated-field carve-out.
  - [x] Characterization tests pinning the polling nodes *(done 2026-08-27; `PollingNodeCharacterizationTests`)*
  - [ ] `Continuation` + `RunnableNode.OnContinuation` + the 10 mechanical sites
  - [ ] The 4 polling nodes, one at a time, each verified against its characterization tests
  - [ ] The choice-callback path (`VisibleChoice` in both printers), then delete `SerializableActionHelper`
  - Prerequisite moved to M2: the two `GetHashCode()`-based unstable keys.

---

### M4 — Saveable refactor, then one smoke *(~4–6 evenings)*

- [ ] **Saveable refactor — one component + drivers** — see
  [`docs/SAVEABLE_REFACTOR_HANDOFF.md`](docs/SAVEABLE_REFACTOR_HANDOFF.md) for the full design.
  The real reason to do it: the current key is `$"{field} {transform.FullPath()}[{componentIndex}]"`
  (`SerializableMonoBehaviour.cs:22`), so renaming an object, reparenting it, reordering components, or two
  same-named siblings **silently breaks the save** — a latent correctness bug in the headline feature, and the
  single strongest reason this section goes before the launch rather than after.
  Keep driver `StateType`s as simple serializer-friendly structs so they survive the serializer swap.
  - [ ] `Saveable` + stable GUID (mint at author time; duplicate-paste detection — copy Unity's open-source
    `GuidComponent` approach; prefab assets carry no guid)
  - [ ] **Fold in the fork's noun-instance init-order fixes** — `[DefaultExecutionOrder(-100)]` and an
    idempotence guard on `CreateNounForGameObject.Start()`, plus lazy creation in
    `SerializeNounInstanceReference.GetInstance()`. Fixes a real race (anything asking for a noun instance
    during another component's `Awake`/`Start` gets null today), but it overlaps this task's GUID identity
    work — fold it in rather than porting it standalone. *(from inbox 2026-08-11 — Tier B8)*
  - [ ] `IComponentSaver` registry + the 9 built-in drivers (port the `Serialize*` table)
    - [ ] Port the fork's `SerializeNavMeshAgent` as the **reference driver** — it's the best real-world
      hand-written save driver in either tree (serializes agent enabled/stoppingDistance plus either a
      destination `Vector3` or a `Transform` full-path, and resumes a `Promise` on arrival). Ships as a
      sample/driver, not core. *(from inbox 2026-08-11 — Tier C)*
  - [ ] `[Save]` behavior-field scan (rename of `[SerializeMonoBehaviourField]`), incl. `NodeRunner`/`Promise`
    side-table routing under the guid key
  - [ ] Migrate `RunActionVerbMonoBehaviour` + `NarramancerScene` to the guid model
  - [ ] Spawned-object guid reassignment in `SerializableSpawner`
  - [ ] Update the 4 sample scenes; EditMode tests per driver
- [ ] **Re-run the Task Z build smoke** — **required, not optional**, and this is the one that covers M2, M3
  and M4 together. Repeat the full round-trip (run → suspend → save → close the app → reopen → load → resume)
  on a **WebGL build** at minimum, since it's IL2CPP-only and therefore covers AOT codegen + managed stripping
  in one shot. See `done.md` for why WebGL is the decisive build.

---

- [ ] **Multiple xNode editor windows** — non-breaking and independent, so it gates nothing. Room to ride along
  before the launch and give the relaunch a genuinely new feature to tout, but **don't let it delay §3.**
  ~2–3 evenings: four specific pieces of global state to make per-window, plus one known limitation to accept
  rather than fix. **Read [`docs/V1_ROADMAP.md`](docs/V1_ROADMAP.md) §4/B6 before starting** — it has the
  file:line breakdown.

### Editor & authoring quality — non-breaking riders

No save-format impact, so these gate nothing and can run in parallel with the work above. Do them in the
order listed: the first is infrastructure the next two depend on. All four are ports —
see [`docs/UPSTREAM_PORT_SCOPE.md`](docs/UPSTREAM_PORT_SCOPE.md) Tier B.

- [ ] **`EditorDrawerUtilities`: resolve nested `SerializedProperty` paths** — `GetTargetObject`/`GetFieldInfo`
  only handle top-level fields, so any drawer used on a field *inside* a serializable class silently fails.
  Walk dotted property paths, and add `GetParentType()`, `GetPropertyTargetType()`, `GetTargetObjectParent()`,
  `GetFirstFieldWithType<T>()`. Then fix `VerbGraphDrawer`, which resolves its attribute lookup against the
  wrong type. **Infrastructure — do this first.** *(from inbox 2026-08-11 — Tier B4)*
- [ ] **`VariableAssignment` partial-match recovery** — renaming a verb input currently **wipes** every
  assignment bound to it, silently, across every graph and component that referenced it. Match on *either* id
  or name (with a type match) and repair the stale half. Pure authoring-quality fix. Optionally ships with the
  `VariableAssignmentList` + drawer that wraps the same logic for reuse. *(needs the nested-path work above)*
  *(from inbox 2026-08-11 — Tier B5)*
- [ ] **`ChooseRankedWeightedActionNode` doesn't forward graph inputs** — missing both `UpdatePorts()` (so the
  node never grows ports for its actions' verb inputs) and `AssignGraphVariableInputs()` (so the chosen effect
  graph runs with unassigned inputs). Effectively only usable with zero-input action verbs today. Match the
  pattern already used by `OfferObjectsAsChoicesNode`/`ListFilterNode`. *(from inbox 2026-08-11 — Tier B7)*
- [ ] **Graph editor navigation** — a History dropdown beside Back listing the recently-opened stack, and a
  corrected Back stack (the current one mutates the stack in `OnOpen()` and loses position). Plus live
  `NodeRunner` inspection of `NarramancerSingleton` during play, which fills the two in-code
  `// TODO: include NarramancerSingleton` comments in `VerbGraphEditor.cs` and is directly useful when
  recording the §3 demo video. Skip the fork's `NARRAMANCER_SHOW_FPS` block — debug scaffolding.
  *(from inbox 2026-08-11 — Tier B6)*

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
  it's feature documentation. Repos rank in Google for queries the Asset Store page never will. Add a short
  licensing line pointing at [`LICENSING.md`](LICENSING.md) — "free for noncommercial use; commercial use needs
  an Asset Store license" is a feature, stated plainly.
  - ⚠️ **Say "source-available," never "open source"** — here, on the listing, on narramancer.com, and in any
    forum post. PolyForm Noncommercial is not OSI-approved, and the distinction gets policed hard in developer
    communities. Getting it wrong turns a selling point into an argument.
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
- [`docs/ODIN_REMOVAL_PLAN.md`](docs/ODIN_REMOVAL_PLAN.md) — feature inventory read off a real save file, the
  gaps in v2's serializer, the two landmines (instanceID refs, serialized closures), and a 6-phase plan.
- [`docs/SAVEABLE_REFACTOR_HANDOFF.md`](docs/SAVEABLE_REFACTOR_HANDOFF.md) — full design for the single
  `Saveable` component + driver registry + GUID identity.
- [`docs/V1_IMPROVEMENT_PLAN.md`](docs/V1_IMPROVEMENT_PLAN.md) — the original 5-item engineering plan.
  Superseded on sequencing by `V1_ROADMAP.md`; still the reference for items #1–#5 rationale.
- [`docs/WEBGL_SAVE_PERSISTENCE_FIX.md`](docs/WEBGL_SAVE_PERSISTENCE_FIX.md) — the `FS.syncfs` fix. Landed;
  kept for the root-cause explanation.
- [`docs/REBUILD_PLAN.md`](docs/REBUILD_PLAN.md) — **v2, paused at phase 2 of ~8.** Not active work. Referenced
  only as the R&D kit the v1 items harvest from (`NarraSerializer`, the Core-split pattern, `TypeCache` spec).
- [`docs/UPSTREAM_PORT_SCOPE.md`](docs/UPSTREAM_PORT_SCOPE.md) — survey of the plugin fork living in the
  *Pseudo World Gaia* project (32 new files, 54 modified), tiered by value with a recommended landing order.
  The Tier A/B/C letters cited throughout this file refer to it. Also records what **not** to port — the
  fork is frozen around Nov 2025, so several of its diffs are this repo being ahead, and a dozen of its files
  depend on paid Odin Inspector.
- [`docs/task-system-handoff.md`](docs/task-system-handoff.md) — how this three-file system works.

---

## Future / Nice to Have

- [ ] **Extract `Narramancer.Core`** — a `noEngineReferences` netstandard2.1 assembly for the serializer,
  domain model, and utilities, with a `dotnet/` mirror for fast Unity-free CI. **Stops at the xNode boundary**
  — nodes derive from xNode's `Node : ScriptableObject` and cannot move; pushing past that boundary *is* the v2
  rewrite. Genuine "harden for the long haul" value, but no buyer perceives it. **Skip entirely if the 30-day
  measurement comes back flat.**
- [ ] **Tier C generic node adds (port-back)** — `MinFloatNode`/`MaxFloatNode`,
  `GetDistanceBetweenGameObjectsNode`, `RemoveBlackboardVariableNode`/`RemoveInstanceBlackboardVariableNode`
  (the `Blackboard.Remove(key, Type)` API already exists here — the nodes were simply never written, an obvious
  gap next to the existing Set/Get nodes), `ClearRunningNodesNode`, `FirstTimeConditionalNode`,
  `PrefabNounIngredient`, `SetImageFillToStat`. Each is small and dependency-free. Cheap riders on any of the
  work above rather than an evening of their own. *(from inbox 2026-08-11 — Tier C)*
- [ ] **Choice-authoring ergonomics + small API additions (port-back)** — `OfferChoicesNode.AddChoiceNode`
  gaining add-at-top as well as add-at-bottom; `Blackboard.UniqueKey(string, params object[])`;
  `NounInstance.HasProperties`/`IsValid` and `AddRelationship` returning the instance instead of dropping the
  call; `NarramancerSingleton.GetInstance(GameObject)`/`GetInstancesWithProperty`; `ListExtensions.TryChooseOne`;
  moving `Instancable` out of `CreateInstanceNode` into `IInstancable.cs` where it belongs. Grab these
  opportunistically when touching the surrounding file. *(from inbox 2026-08-11 — Tier B9/B10)*
- [ ] **Triage the 28 in-code `// TODO` comments** — mostly node-level polish and editor papercuts (predicate
  options on `OfferObjectsAsChoicesNode`, name-input dialogs in `ActionVerbListDrawer`, stat handling without
  min/max in `StatInstance`). Worth one pass to promote the few that matter and delete the rest, rather than
  seeding 28 lines here.
- [ ] **Rename the publisher from "Professional Bad Guys"** — signals nothing relevant to someone evaluating a
  save system. Low priority.

---

_See [`done.md`](done.md) for the archive of completed work._
