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

**Where things stand (2026-10-28).** All of §2's code is done and on `main`: M1–M4, the pre-launch fixes, the
Tier B authoring riders, the sample launcher, the rewritten Getting Started guide and the UI theme. 573/573
EditMode tests pass. The WebGL build smoke passed on 2026-10-28, but that was **before** the UI pass, so the
restyled UI has only been checked in the editor. What remains before §3 is mostly yours to run:

- [x] **The patch test for M2–M4 — passed 2026-10-30, all green.** Save, change the assets, rebuild, load (§2, "Re-run the Task Z
  build smoke"; procedure in [`docs/RELEASE_RUNBOOK.md`](docs/RELEASE_RUNBOOK.md)). It is all that stands
  between here and the relaunch work. Scene objects have a durable identity, `Saveable` is the one component
  that saves an object, and eight of the nine `Serialize*` components are gone (`SerializeNounInstanceReference`
  stays). Design record: [`docs/SAVEABLE_REFACTOR_HANDOFF.md`](docs/SAVEABLE_REFACTOR_HANDOFF.md).
- [ ] **A build check of the 2026-10-28 UI pass** — fold it into the patch-test build. Look at the save and load
  menus, the top-right button row, the samples' title cards, and the launcher's **back button**, which has only
  been seen in code (camera screenshots leave out IMGUI).
- [ ] **Upload the video and fill in the 1.0.0 submission** — every field, paste-ready, is in
  [`docs/LAUNCH_COPY.md`](docs/LAUNCH_COPY.md) (2026-10-29): the YouTube title, description and chapters; the
  package name, category, price, compatibility, keywords, summary, description, technical details and release
  notes; and which file goes in which media slot. Media is in [`docs/media/`](docs/media/), including the card,
  cover and social marketing images (rebuilt by `~/Videos/narramancer-demo/_edit/store_images.py`). Upload to
  YouTube as **Unlisted** until approval. Submitting still waits on the patch test. Once the video is up, put its
  link into `README.md` in place of `YOUTUBE_ID`.
  - *2026-10-30: video uploaded (Unlisted) and the 1.0.0 submission is with Unity for review.* On approval:
    make the video Public, push the README (with the video link) and deploy narramancer.com **on the same day**,
    then start the 30-day window. Nothing ships during it.
- [ ] **Odin Inspector coexistence check**: still yours, listed below. Do it before saying anything about Odin
  publicly; the release notes deliberately don't.

**narramancer.com** has its own handoff for a session in the site repo:
[`docs/NARRAMANCER_COM_HANDOFF.md`](docs/NARRAMANCER_COM_HANDOFF.md). Deploy it on approval day.

**Claude can pick up any time without you:** the rest of **Graph editor navigation** (§2, riders), **Multiple
xNode editor windows**, **A10**, and editor screenshots for the Getting Started guide (the video is cut).

⚠️ **Read the code before trusting the task notes.** Four premises have now turned out to be stale: `Cancel`
"overridden by nothing" (three nodes override it), "two unstable keys" (thirteen), the Promise registry being
dead code (it is load-bearing), and timer cancellation being cosmetic (it was not). Treat the remaining Tier B
notes as leads, not specs.

---

## Decisions needed

_None open. Both were resolved 2026-08-11 — see [`done.md`](done.md)._

---

## 1. Pre-work — do now, in parallel

Free, and **exposure-neutral**: none of this makes the product more discoverable or purchasable, so it doesn't
compromise the "land the breaking changes before anyone can buy" ordering. The baseline in particular has to be
recorded before anything changes anyway.

- [x] **Record the views/day baseline** — *recorded 2026-10-30 from the publisher dashboard export, before the
  1.0.0 listing went live.* **2025-12-01 → 2026-10-28 (332 days): 336 pageviews, ≈1.0/day.** 0 sales, 0 wishlists,
  0 refunds, no rating. That is well below the ~3–4/day this file used to assume, so compare against **~1/day**.
  The export is a single total row, so it gives no referrer breakdown and no daily trend. At ~1/day a 30-day window
  expects about 30 views (±~6 from noise alone), so 2× or more is a real signal and +20% is not.
- [ ] **Verify the listing's Description / Technical Details are populated at all** — *re-fetched 2026-10-29: still empty, and the 1.0.0 copy fills both regardless.* They rendered empty on
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
reason this section moved ahead of the launch. The test net that used to gate this work **exists** — 573
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

- [~] **The test net is up — the gate on the refactors below is CLOSED.** `Tests.Editor` is at **573 passing**
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

- [x] **M4a — guid identity (the correctness bug)** — see the **Reassessment** at the top of
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
    every spawned object. Now covered by `SceneComponentSaveTests` *(2026-10-27)*.
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
  - [x] **Migrate the 4 sample scenes** — committed 2026-10-27 (`ea60727`). Original note: As of 2026-10-27 the working tree has
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
  - [x] **Build smoke** — passed 2026-10-27 on WebGL, all four samples through the launcher, after the
    load-order fix. The **patch test** is folded into the final build check below rather than run twice.
- [x] **M4b — `Saveable` + driver registry** — done 2026-10-27, archived to [`done.md`](done.md). Left over:
  - [x] **A node drives `NavMeshMover`** *(2026-10-28)* — `MoveNavMeshAgentNode` (*GameObject/Move NavMesh
    Agent*): a destination, or a transform to follow, and an optional wait for arrival. Compiled out without the
    AI module, like the mover. ⚠️ **No automated coverage**: an agent only paths inside the play-mode loop, and
    the suite is EditMode only. The node is thin glue over the mover, but nothing has run it yet. Covered the day
    a sample walks something, or a PlayMode assembly exists.
  - [ ] **A node can write to a component nothing saves, and nothing says so.** Scene 4 hit this: a verb set
    text on objects with no `Saveable`, or with an identity-only one, and the text quietly reverted on load. A
    play-mode warning would have to tell deliberate transient UI apart from a mistake, so it is likely too noisy.
    An editor-side check, "this verb writes to X, and X doesn't save text", can't see objects found at runtime
    by `FindChildTransform`. No good answer yet; worth a paragraph in the docs at minimum.
  - [ ] **A project upgrading from 0.5.x would open with missing scripts** where the `Serialize*` components
    were — there is no converter, because the conversion was done by hand on the one sample that used them.
    Accepted at ~0 users; worth one line in the release notes, and a converter keyed on the old script GUIDs
    (recorded in `done.md`) if anyone asks.
- [x] **Re-run the Task Z build smoke, and the patch test** — **both passed** (2026-10-28, 2026-10-30). **required, not optional**, and this is the one
  that covers M2, M3 and M4 together. For M4b, confirm scene 4's sprites **and** texts come back after a reload.
  - [x] **Build smoke passed 2026-10-28** on WebGL, all four samples through the launcher, after the two fixes
    below. Merged to `main`. That build predates the UI pass.
  - [x] **Patch test passed 2026-10-30** against M4: save, change the assets, rebuild, load.
  - The first run found two scene-4 bugs (sprites missing from the manifest, texts never saved); both fixed —
    see [`done.md`](done.md), 2026-10-28.
  ⚠️ **No sample tweens, animates or nav-moves anything**, so no build can exercise a `Tweener` being re-added
  on load — the M4b path most exposed to IL2CPP, since it finds the type by name. Either add a short tween to a
  sample (a "walk on" in scene 4 would also be demo-video material) and save mid-move, or accept the EditMode
  coverage for now. Repeat the full round-trip (run → suspend → save → close the app → reopen → load → resume)
  on a **WebGL build** at minimum, since it's IL2CPP-only and therefore covers AOT codegen + managed stripping
  in one shot. See `done.md` for why WebGL is the decisive build.

### Pre-launch fixes — small, non-breaking

The 2026-10-28 batch (continue indicator, launcher, Getting Started guide, UI pass) is archived in
[`done.md`](done.md).

- [ ] **Multiple xNode editor windows** — non-breaking and independent, so it gates nothing. Room to ride along
  before the launch and give the relaunch a genuinely new feature to tout, but **don't let it delay §3.**
  ~2–3 evenings: four specific pieces of global state to make per-window, plus one known limitation to accept
  rather than fix. Also fix vendored xNode's own Unity 6 deprecation warnings while in there —
  `NodeEditorUtilities.cs:267,277` (`EndNameEditAction`, `StartNameEditingIfProjectWindowExists`) and
  `NodeEditorWindow.cs:196` (`InstanceIDToObject` → `EntityIdToObject`). *(from inbox 2026-10-27)* **Read [`docs/V1_ROADMAP.md`](docs/V1_ROADMAP.md) §4/B6 before starting** — it has the
  file:line breakdown.

### Editor & authoring quality — non-breaking riders

No save-format impact, so these gate nothing. Tier B4, B5 and B7 landed 2026-10-28 and are archived in
[`done.md`](done.md); B6 is the one left. Ports from the fork, see
[`docs/UPSTREAM_PORT_SCOPE.md`](docs/UPSTREAM_PORT_SCOPE.md) Tier B.

- [ ] **Graph editor navigation** — *partial fix 2026-10-28:* after a load the graph highlighted the
  pre-load runner forever. The window kept the runner it was showing until you picked another, and a load
  replaces every runner. It now drops a runner that is no longer on offer. This matters because it is the
  demo video's key shot. Checked in play mode by saving, carrying on and loading; it was seen stale without the
  fix. *Toolbar pass 2026-10-29:* the loose buttons and the side dropdown became a real toolbar. It has
  Back, a **Recent** dropdown (the History dropdown this task asked for), an asset › verb breadcrumb whose
  menu switches between verbs in the same asset, **Linked** (the verbs this one uses and the verbs that use it),
  Find Verb, and Add Node / Frame / Collapse / New Child Verb / More. While playing, the runner pickers sit in a
  second row. Clicks on the toolbar no longer fall through to the graph, which used to clear the selection.
  The fix is `NodeGraphEditor.IsOverlayAt` in vendored xNode. **Compiled, but not yet looked at in the
  editor.** *Seen again 2026-10-29 while recording:* in one of two takes of a load after an editor restart, the
  graph lit the first Print Text and never moved, though the story had resumed at Offer Choices
  (`~/Videos/shot1-2026-10-29_16.39.00.mkv`, about 0:18). The other take followed correctly. So the stale
  highlight is intermittent, not fixed. **Still open:** a corrected Back stack (the current one mutates the stack in `OnOpen()` and
  loses position). Plus live
  `NodeRunner` inspection of `NarramancerSingleton` during play, which fills the two in-code
  `// TODO: include NarramancerSingleton` comments in `VerbGraphEditor.cs` and is directly useful when
  recording the §3 demo video. Skip the fork's `NARRAMANCER_SHOW_FPS` block — debug scaffolding.
  *(from inbox 2026-08-11 — Tier B6)*

---

## 3. Launch — the relaunch bundle (ships as 1.0.0)

Everything here goes out together, on a build that already has the new serializer and the `Saveable` model.
**Nothing in this section starts until §2 is done and the build smoke has been re-run.**

- [x] **Record the demo video** — cut approved 2026-10-29; shot list, setup and edit plan in [`docs/DEMO_VIDEO_PLAN.md`](docs/DEMO_VIDEO_PLAN.md) (2026-10-28): you record the raw clips, Claude edits them with ffmpeg. 60–90s, no narration: run a graph → pause mid-execution → save → quit the
  editor entirely → reopen → load → resume exactly where it was. **Record it after the Saveable refactor**, so
  any inspector footage shows the single `Saveable` component rather than the old `Serialize*` zoo and doesn't
  need re-shooting. Use on the listing, README, narramancer.com, and as the r/Unity3D post.
- [ ] **Republish as 1.0.0 with Unity 6 compatibility** — drop the 0.x (loudest "unfinished" signal), declare
  6000.x, then verify the live compatibility table actually shows Unity 6. Success check: the asset appears
  when browsing Visual Scripting filtered to 6000.x.
- [~] **Rewrite the listing around save/load** — *drafted 2026-10-29 in [`docs/LAUNCH_COPY.md`](docs/LAUNCH_COPY.md), not yet submitted.* — lead with the pain, not the mechanism. "Node graph tool" is a
  commodity with a free first-party incumbent; "graph tool with save/restore built into the execution model"
  is close to a category of one. Title → `Narramancer — Node Graphs with Built-In Save/Load`.
- [ ] **Price → $39** — expect this to *raise* conversions. No launch discount; $7.99 reads as abandonware.
- [ ] **Replace the keyword set** — drop unwinnable generics (`logic`, `Tool`, `Node`, `flow`, `visual`,
  `Graph`) for high-intent terms: `save system`, `save and load`, `game state`, `serialization`, `checkpoint`,
  `save anywhere`, `visual novel`, `dialogue`. Also confirm which category is primary.
- [~] **Rewrite README as a landing page** — *done 2026-10-29 except the YouTube link (`YOUTUBE_ID`); hold the push until approval day.* — demo GIF at top, one paragraph, link to the listing. Currently
  it's feature documentation. Repos rank in Google for queries the Asset Store page never will. Add a short
  licensing line pointing at [`LICENSING.md`](LICENSING.md) — "free for noncommercial use; commercial use needs
  an Asset Store license" is a feature, stated plainly.
  - ⚠️ **Say "source-available," never "open source"** — here, on the listing, on narramancer.com, and in any
    forum post. PolyForm Noncommercial is not OSI-approved, and the distinction gets policed hard in developer
    communities. Getting it wrong turns a selling point into an argument.
- [ ] **narramancer.com: move the Naninovel recommendation** — *handoff: [`docs/NARRAMANCER_COM_HANDOFF.md`](docs/NARRAMANCER_COM_HANDOFF.md).* — it currently talks visual-novel devs out of the
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
