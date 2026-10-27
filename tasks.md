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

- [ ] **Finish M4a — scene objects get a durable identity.** M1, M2 and M3 are all done as of 2026-09-04, each
  verified on a real WebGL player. M4 is the last of the breaking work before §3, split in two: **M4a is the
  bug** (path-based save keys), **M4b is the feature** (one component + drivers). M4a is most of the way there —
  what's left is listed under §2 M4a below, ending in the build smoke + patch test. Design and corrections:
  [`docs/SAVEABLE_REFACTOR_HANDOFF.md`](docs/SAVEABLE_REFACTOR_HANDOFF.md).
  *Status re-verified against the code 2026-10-27, after a ~7-week gap: 518/518 EditMode tests passing.*
  ⚠️ **Read the code before trusting the task notes.** Four premises have now turned out to be stale — `Cancel`
  "overridden by nothing" (three nodes override it), "two unstable keys" (thirteen), the Promise registry being
  dead code (it is load-bearing), and timer cancellation being cosmetic (it was not). Treat the remaining
  Tier B notes as leads, not specs.

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
reason this section moved ahead of the launch. The test net that used to gate this work **exists** — 518
EditMode tests (2026-10-27), run with `run_tests` (EditMode, assembly `Tests.Editor`) — so nothing here is blocked on it
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

- [~] **The test net is up — the gate on the refactors below is CLOSED.** `Tests.Editor` is at **518 passing**
  EditMode tests (2026-10-27); run with `run_tests` (EditMode, assembly `Tests.Editor`). Everything in this section that
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

### M2 — Execution-model correctness ✅ *(done 2026-09-04)*

Sub-runner ownership, durable blackboard keys, and both riders. Restructuring the sub-runner nodes here means
M3 touches them once rather than twice.

- [x] **Sub-runner tree + node cancellation** *(done 2026-09-04)*. A runner now records the runners it starts,
  and `StopAndReset()` stops and releases them depth-first — so stopping a runner mid-flight no longer abandons
  everything below it, still being ticked by `UpdateNodeRunners` against a blackboard nobody owns.
  `NarramancerSingleton.CreateSubRunner(name, parent)` is the way to start one. 10 tests in `SubRunnerTests`.
  - **Ownership is recorded, never recomputed.** That was the actual bug: nodes rebuilt a sub-runner's name
    from hash codes at cancel time, so after a reload `ChoosePrioritizedBranchNode` silently failed to cancel
    and `ParallelNode` threw a `NullReferenceException`. Both `Cancel` overrides shrank — `ParallelNode`'s is
    gone entirely, `ChoosePrioritizedBranchNode`'s only clears its own flag now.
  - **Stored by *name*, not by reference** — and that is not a retreat to the old scheme. A stored name travels
    with the save and is never recomputed, exactly as the runner table has always been keyed. Holding
    `NodeRunner` references does not work: `NodeRunner` is also serialized by *Unity*
    (`NarramancerSingleton.storyInstance` is a `[SerializeField]`), which serializes nested plain classes by
    value with a depth limit of 10 — the first attempt blew that limit, and would have come back from a domain
    reload as copies rather than the objects the runner table holds.
  - **`CreateNodeRunner` no longer throws on a duplicate name.** The table is a dictionary and `Add` throws;
    `RunActionVerbForDurationNode` names its sub-runner `"{node} - {graph}"`, the same string every time, so
    two concurrent instances of that node were a crash.
  - [x] **`Blackboard.UniqueKey` no longer keys on the instanceID** *(2026-09-04)*. Bigger than the two sites
    the task claimed: **13 call sites across 9 node types** — loop indices, spawned objects, timers, chosen
    elements. And `UnityEngine.Object.GetHashCode()` **is** `GetInstanceID()` (verified, not assumed), so this
    was the *same* bug as the saved references, hiding in blackboard keys: every one of those changed meaning
    when the build changed, and the state read back as a default. Now keyed by durable id through
    `Serialization.PersistentId`, which shares the save system's resolver so keys and references cannot
    disagree. An owner with no durable id still works for the session but logs a warning saying it will not
    survive a save.
  - [x] **Timer cancellation** — done in M3 (`eb1851d`): `SerializableTimer.ownerKey` records who started a
    timer, so cancelling a node removes its timer instead of leaving it to fire against a stopped runner. A
    follow-up (`1ac410e`) fixed saved timers being anchored to the previous session's clock.

**Riders — both done 2026-09-04:**

- [x] **`InequalityNode` comparison semantics fixed** — `GreaterThan` evaluated `>=` and `LessThan` evaluated
  `<=`. The two misnamed members keep their enum **indices** (renamed to `GreaterThanOrEqualTo` /
  `LessThanOrEqualTo`) and the strict operators are appended at 3 and 4, so every authored graph keeps its
  exact behaviour and gains a correct label. Unity serializes an enum by its integer value, so that ordering
  is load-bearing — `TheComparisonEnumIndicesAreFrozen` asserts it rather than trusting a comment.
  *(Its sibling `CompareFloatNode` was already correct — checked while here.)*
- [x] **Regression tests for the Tier A fixes** — 18 tests in `TierARegressionTests`: the `StatInstance`
  min+max clamp, `NounInstancesQuery` null/empty equality **and** hash agreement (it is a dictionary key), the
  `AssemblyUtilities` nested-namespace AQN regex, `AddRelationshipNode`'s null guard, and the
  `InequalityNode` operators above. The two editor-UI fixes (A6 port style, A8 icon) are deliberately left
  uncovered — a test for either would assert more about Unity's inspector than about this code.
  *Verified by reintroducing the `StatInstance` bug and watching the test fail with the original signature
  (expected 5, got 0) while its two siblings still passed* — which is exactly the shape of "only the max was
  honoured". A regression test written after the fix is worth nothing until it has been seen to fail.

---

### M3 — Replace the serialized closures ✅ *(done 2026-09-04)*

- [x] **Replace serialized closures with a `Continuation` record — done, archived to [`done.md`](done.md).**
  All 18 sites converted, `SerializableActionHelper` deleted, and the WebGL build smoke passed on 2026-09-04.

---

### M4 — Scene-object identity, then the driver refactor *(~5–7 evenings)*

Split on 2026-09-04 after re-reading the plan against the code: **M4a is a live correctness bug, M4b is an
ergonomics feature.** Both belong in the zero-user window, but M4a is independently verifiable and is the half
that must ship if anything gets time-boxed. Full reasoning in the Reassessment at the top of
[`docs/SAVEABLE_REFACTOR_HANDOFF.md`](docs/SAVEABLE_REFACTOR_HANDOFF.md).

- [ ] **M4a — guid identity (the correctness bug)** — see the **Reassessment** at the top of
  [`docs/SAVEABLE_REFACTOR_HANDOFF.md`](docs/SAVEABLE_REFACTOR_HANDOFF.md), which corrects the plan below it.
  A scene object's save key is `$"{field} {transform.FullPath()}[{componentIndex}]"`
  (`SerializableMonoBehaviour.cs:22`), so renaming an object, reparenting it, reordering its components, or two
  same-named siblings **silently break the save**. Same family as every other identity bug this quarter.
  - [x] `Saveable` component holding a minted guid *(2026-09-04)*, with duplicate-paste detection.
    ⚠️ **Minting is a public `EnsureGuid()`, not just lifecycle callbacks.** `Reset`/`OnValidate`/`Awake` are
    Unity's to call: none run for an object built in an EditMode test, and `Instantiate` does not run `Awake`
    outside play mode. Identity that only exists if a callback happened to fire cannot be tested and can be
    missed — the callbacks now just make it happen *early*, so the guid is serialized at author time.
  - [x] `SerializableMonoBehaviour.Key` is now `$"{guid}:{componentType}:{key}"` *(2026-09-04)* — one change
    covering **all 16 subclasses**, since `Key` was the single identity source and nothing overrides it. ⚠️ The four the plan never named all carry live state: `TextPrinter`, `ChoicePrinter`,
    `CreateNounForGameObject`, `SetGlobalVariablesMonoBehaviour`. The first two are the text/choice UI that
    resumes a story after a load — leaving them path-keyed while everything else moves is worse than not
    starting.
  - [x] **`SerializableSpawner` reassigns guids on re-spawn** *(2026-09-04)* — records every `Saveable` in
    each spawn's hierarchy and puts the ids back before reading state, and warns if the prefab's shape changed
    since the save. Was filed as deferrable step 5; doing the key switch without it would have silently broken
    every spawned object. It currently
    restores identity with `newSpawn.name = name`, which works *only* because keys contain `FullPath()`. Switch
    to guids without this and every spawned object's state silently stops loading. The plan files it as
    deferrable step 5; it is not.
  - [ ] **`NarramancerScene` still names its runners by `verb.name`** (`NarramancerScene.cs:71`).
    `RunActionVerbMonoBehaviour` is already done — it inherits the guid `Key()` and names its runner
    `Key(nameof(runner))`. `NarramancerScene`'s run-on-start runners are keyed by bare verb name, so two scenes
    (or a scene and anything else) running the same verb share one runner-table entry.
  - [ ] **Fold in the fork's noun-instance init-order fixes** — `[DefaultExecutionOrder(-100)]` and an
    idempotence guard on `CreateNounForGameObject.Start()`, plus lazy creation in
    `SerializeNounInstanceReference.GetInstance()`. Fixes a real race, and overlaps this task's identity work.
    *(from inbox 2026-08-11 — Tier B8)*
  - [x] EditMode tests *(2026-09-04)* — 10 in `SaveableIdentityTests`, verified against the old key: rename,
    reparent and same-named siblings all fail without the fix, and two siblings genuinely produced the
    *identical* key `"position /Parent/Item[2]"`, which is the silent-overwrite case.
    - ⚠️ The reorder test was **vacuous at first** — adding a component leaves existing indices alone, so it
      passed under the old key too. It now uses `ComponentUtility.MoveComponentUp`.
  - [x] Migration helper *(2026-09-04)* — `Window/Narramancer/Add Missing Saveables`, separate passes for open
    scenes and for prefabs. Also the upgrade path for anyone with an existing project, which is why it reports
    what it changed rather than fixing silently.
    - ⚠️ It mints on objects that **already had** a `Saveable`, not only ones it adds. A scene instance of a
      prefab inherits the component but not an id — the asset deliberately has none — so "already has a
      Saveable" does not mean "already has an identity". The first version missed this.
    - ⚠️ It also has to find objects that carry **no saver at all** — a UI `Text` a verb writes into has
      nothing on it saying "I am saved"; what makes it saved is a `VariableAssignment` pointing at it from
      elsewhere in the scene. Two versions got this wrong before it worked: the first looked for savers only,
      the second reflected over each component's own fields and so found only assignments held *directly*.
      Character Equipment holds its five three levels down (`NarramancerScene.variables` → a list wrapper →
      the port → the assignment) and the tool called the scene clean. It now iterates the `SerializedObject`,
      which is Unity's own view of the nesting. `SaveableMigrationTests` covers the shapes; the two nesting
      tests were confirmed to fail against the old scan.
  - [x] Prefabs migrated *(2026-09-04)* — the three printer canvases (`ChoicePrinter`, `TextAndChoicePrinter`,
    `TextPrinter`) now carry a `Saveable` with **no id**, verified in the asset files. Those are the text and
    choice UI whose state resumes a story after a load.
  - [~] **Migrate the 4 sample scenes — run, but not committed.** As of 2026-10-27 the working tree has
    minted `Saveable`s in all four (1 each in scenes 1–3 plus a prefab-instance override in scene 1; 16 in
    scene 4). **Review before committing:** scene 1's diff also carries RectTransform layout overrides on the
    printer prefab — possibly the "unsaved smoke-test changes" this task was waiting on — and scene 4 has 16
    against the 15 expected (10 savers + 5 verb-wired).
  - [x] Round-trip test *(2026-09-04)*, and the build smoke passed on a real player.
  - [x] **A save can now point at a scene object** *(2026-09-04)* — pulled forward from Future because a bug
    report demanded it. `SceneObjectIds` adds a fourth id form, `scene:<guid>` (plus the component type, since
    one object can hold several), resolving through `Saveable`'s claim table. `Blackboard.gameObjects` and
    `.components` are **no longer `[NonSerialized]`** — they had been since the first commit in 2023, because
    a scene object had no identity to name.
    - This is what fixed the Character Equipment sample's text: the scene keeps its UI `Text` objects in a
      blackboard, those references were dropped on save, and `SetTextNode` then found null and did nothing —
      labels at their scene defaults, nothing in the log. A scene object that still has no `Saveable` is now
      reported rather than dropped.
  - [ ] **No test covers `SerializableSpawner` putting guids back on re-spawn** — the riskiest part of M4a,
    and the one where a regression is silent (every spawned object's state just stops loading).
  - [ ] **Build smoke + the patch test** — identity changed, so both are owed.
    [`docs/RELEASE_RUNBOOK.md`](docs/RELEASE_RUNBOOK.md).
- [ ] **M4b — `Saveable` + driver registry (the ergonomics)** — one component per object driving a registry of
  savers, so adding an object is "drop one component" and supporting a new component type is "write one
  driver". Real value, but **no urgency of its own** — the correctness half is M4a. Still belongs in the
  zero-user window, because collapsing 9 components into drivers also changes which keys exist.
  - [ ] `IComponentSaver` + `ComponentSaverRegistry` + the 9 built-in drivers.
    - [ ] **`SerializeImage` / `SerializeSpriteRenderer` rebuild a sprite instead of referencing one** — they
      save texture, rect and pivot, then `Sprite.Create(...)` on load: a *new* sprite every load, losing border,
      pixelsPerUnit and atlas packing, and breaking anything that compares sprites by reference. Asset
      references are GUID-keyed now, so the driver can just save the sprite. Changes the save format, so do it
      as part of the driver rewrite. *(from inbox 2026-10-27)*
    - [ ] Port the fork's `SerializeNavMeshAgent` as the **reference driver** — the best real-world save driver
      in either tree. Ships as a sample, not core. *(from inbox 2026-08-11 — Tier C)*
  - [ ] `[Save]` behaviour-field scan (rename of `[SerializeMonoBehaviourField]`).
  - [ ] ⚠️ `SerializeTransform` carries **eight** `[SerializeMonoBehaviourField]`s of tween state. "Tween logic
    moves to a behaviour component" is the largest single item in the driver table and the plan understates it.
  - [ ] Auto-detect profile + inspector opt-out.
- [ ] **Re-run the Task Z build smoke** — **required, not optional**, and this is the one that covers M2, M3
  and M4 together. Repeat the full round-trip (run → suspend → save → close the app → reopen → load → resume)
  on a **WebGL build** at minimum, since it's IL2CPP-only and therefore covers AOT codegen + managed stripping
  in one shot. See `done.md` for why WebGL is the decisive build.

### Pre-launch fixes — small, non-breaking

- [ ] **The continue indicator shows while a `Wait` is running after a load** — reloading into a suspended
  `WaitNode` displays `TextPrinter`'s continue indicator, so it looks like it wants a click when it is actually
  waiting out a timer. Cosmetic, but it lands on the exact moment the plugin is sold on — and the demo video
  will show it. Likely `TextPrinter` restoring its visual state without knowing whether the promise it is
  waiting on is a timer or a click. *(from inbox 2026-10-27; found 2026-09-04 smoke-testing M3)*
- [ ] **`Blackboard.Set(key, null)` throws a `NullReferenceException`** — the two-argument overload derives the
  type from the value (`Blackboard.cs:442`), so a null has nothing to derive from. The three-argument
  `Set(key, value, type)` handles null. *(from inbox 2026-10-27; found 2026-09-04)*
- [ ] **Rewrite the shipped `Getting Started.pdf`** — `Assets/Narramancer/Documentation/Getting Started.pdf`
  ships in the package and was last touched 2024-02-22, before the new serializer and `Saveable`. It almost
  certainly describes the old `Serialize*` workflow. Do it after M4b settles which components exist.

---

- [ ] **Multiple xNode editor windows** — non-breaking and independent, so it gates nothing. Room to ride along
  before the launch and give the relaunch a genuinely new feature to tout, but **don't let it delay §3.**
  ~2–3 evenings: four specific pieces of global state to make per-window, plus one known limitation to accept
  rather than fix. Also fix vendored xNode's own Unity 6 deprecation warnings while in there —
  `NodeEditorUtilities.cs:267,277` (`EndNameEditAction`, `StartNameEditingIfProjectWindowExists`) and
  `NodeEditorWindow.cs:196` (`InstanceIDToObject` → `EntityIdToObject`). *(from inbox 2026-10-27)* **Read [`docs/V1_ROADMAP.md`](docs/V1_ROADMAP.md) §4/B6 before starting** — it has the
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
  graph runs with unassigned inputs). Effectively only usable with zero-input action verbs today.
  ⚠️ **Not a straight port — size it first:** the single-graph reference cases
  (`OfferObjectsAsChoicesNode`/`ListFilterNode`) don't transfer cleanly, because this node holds a *list* of
  actions, each with its own effect graph. `UpdatePorts()` has to build the union of their inputs and decide
  what happens when two expose same-named inputs of different types — a design decision.
  *(from inbox 2026-08-11 — Tier B7; sizing note from inbox 2026-10-27)*
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

- [x] ~~**Let a save point at a scene object**~~ — **done 2026-09-04**, pulled forward into M4a because the
  Character Equipment sample needed it. See §2 M4a.
- [ ] **Extract `Narramancer.Core`** — a `noEngineReferences` netstandard2.1 assembly for the serializer,
  domain model, and utilities, with a `dotnet/` mirror for fast Unity-free CI. **Stops at the xNode boundary**
  — nodes derive from xNode's `Node : ScriptableObject` and cannot move; pushing past that boundary *is* the v2
  rewrite. Genuine "harden for the long haul" value, but no buyer perceives it. **Skip entirely if the 30-day
  measurement comes back flat.**
- [ ] **Promise registry on the singleton (port-back)** — `NarramancerSingleton.MakePromise()` /
  `BreakPromise()` / `UpdatePromises()` driven from `Update()`, plus `Promise.WithUpdate(Action)`,
  `Promise.removeOnResolve`, and a `Promise.DefaultDone` singleton. A promise that is *polled every frame*
  rather than only resolved.
  ⚠️ **Moved out of §2 on 2026-09-04, because the reason it was there was false.** The task claimed
  `StoryInstance.promises` was dead code that "ships empty", so it belonged in the break-the-save-format
  window. It is neither: `SerializableMonoBehaviour` routes every `[SerializeMonoBehaviourField]` `Promise`
  field through it, `SerializeAnimation` and `SerializeTransform` both declare one, and `SerializeAnimation`
  uses it to save an in-flight animation and resume it on load. The field is already a
  `StringPromiseDictionary`, so adding entries changes no format — **this is a feature with no pre-launch
  urgency.** Judge it on merit. *(from inbox 2026-08-11 — Tier B2)*
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
