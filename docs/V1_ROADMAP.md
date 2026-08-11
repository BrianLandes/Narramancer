# Narramancer v1 — Consolidated Roadmap

> **This is the strategic view** — why the work is ordered this way, what the tradeoffs are, and how the
> marketing and engineering tracks interlock. The **tactical** view (what's next, in order) lives in
> [`../tasks.md`](../tasks.md). Read this once; work from there.
>
> Supersedes the sequencing in [`V1_IMPROVEMENT_PLAN.md`](V1_IMPROVEMENT_PLAN.md) by folding in the
> **validation/marketing handoff** ([`narramancer-v1-handoff.md`](narramancer-v1-handoff.md)) and the
> [`SAVEABLE_REFACTOR_HANDOFF.md`](SAVEABLE_REFACTOR_HANDOFF.md). [`REBUILD_PLAN.md`](REBUILD_PLAN.md)
> describes v2 and stays paused; it is referenced here only as the R&D kit we harvest from.
>
> **The thesis, unchanged:** v1 is a marketing problem wearing an engineering costume. 22 favorites against ~0
> sales means the concept lands and the listing doesn't close. Engineering work earns its place only by
> unblocking or strengthening the listing.

---

## 0. Where things actually stand (verified against the working tree, not the docs)

The planning docs were written assuming Phase 0 hadn't started. Several items have since landed. Current truth:

| Item | What the docs assume | Verified actual state |
|---|---|---|
| Unity 6 upgrade | "BLOCKING GATE — not started" | ✅ **Done.** `ProjectVersion.txt` = `6000.4.12f1`; editor open on it; console clean (0 errors, 0 warnings) |
| WebGL save persistence | Handoff written as a to-do | ✅ **Done.** `Plugins/WebGL/SaveFileSync.jslib` + `Utilities/SaveFileSync.cs` + the flush call at [SaveLoadUtilities.cs:40](../Assets/Narramancer/Scripts/Utilities/SaveLoadUtilities.cs#L40) |
| #5 TypeCache scanner | To do | ❌ Not started — [AssemblyUtilities.cs:16](../Assets/Narramancer/Scripts/Utilities/AssemblyUtilities.cs#L16) still does `AppDomain.CurrentDomain.GetAssemblies()` |
| `FindObjectsOfType` cleanup | To do | ❌ Not started — wrapper at [GameObjectExtensions.cs:13](../Assets/Narramancer/Scripts/Extensions/GameObjectExtensions.cs#L13) + 6 direct sites |
| #3 test net | "one `BlackboardTests.cs`" | ❌ Confirmed — 1 file, 50 lines. No save/load coverage at all |
| #1 Odin removal | To do | ❌ Not started. **Good news: only 2 call sites** ([SaveLoadUtilities.cs:56](../Assets/Narramancer/Scripts/Utilities/SaveLoadUtilities.cs#L56) and [:82](../Assets/Narramancer/Scripts/Utilities/SaveLoadUtilities.cs#L82)) against 186 vendored files |
| #2 Core extraction | To do | ❌ Not started |
| #4 Multi-window xNode | To do, "sizing unknown" | ❌ Not started — **now sized, see §4.4** |
| Saveable refactor | To do | ❌ Not started — 9 `Serialize*` components + `SerializableSpawner` still in place |
| Repo license | "open question" | ✅ **Resolved 2026-08-11** — relicensed to PolyForm Noncommercial 1.0.0 going forward; prior Apache-2.0 versions stay Apache-2.0. See [`../LICENSING.md`](../LICENSING.md) |
| Version | "bump to 1.0.0" | `bundleVersion: 1.0` in ProjectSettings (that's the demo project, not the listing — listing still reads 0.5.7) |

**Scale of the thing:** 25,403 LOC of first-party script (excluding vendored plugins), 156 node types, 77 editor
scripts, 25 components. Vendored: 186 OdinSerializer files, 37 xNode files.

**What this changes:** the "blocking gate" is already through. The critical path is no longer *make it work on
Unity 6* — it's **prove it works in a shipped player, then fix the listing.**

---

## 1. The one genuinely blocking unknown — ✅ RESOLVED 2026-08-11

> **Outcome: passed.** Both a Mono standalone (Linux) and a **WebGL** build round-tripped save → close the app
> → reopen → load → resume. WebGL is the one that settles it: it's IL2CPP-only, so it exercised the AOT codegen
> path this task existed to probe, under managed stripping. A desktop IL2CPP build would be weaker evidence and
> was deliberately skipped. **The marketing track is unblocked; the headline claim is verified on Unity 6 in
> shipped players.** The section below is kept for the reasoning and as the release-smoke runbook.

Everything downstream — the listing rewrite, the demo video, the whole pitch — rests on a claim that has
**never been verified on Unity 6 in a built player**: *save mid-execution, quit, reload, resume.*

The editor compiling clean is not evidence. The historical failure mode for OdinSerializer is AOT/IL2CPP
codegen, which editor play mode never exercises — and Odin's `AOTSupportScanner` + build preprocessor plus
`NarramancerSingleton.OnPreprocessBuild` all run only during a real build.

**Task Z (do this first, before any marketing work):**
1. IL2CPP desktop build of `1. Text and Choices Example`.
2. In the built player: run a verb to a suspend point → save → quit the process → relaunch → load → confirm it
   resumes exactly where it stopped.
3. Repeat for a WebGL build, with a hard refresh between save and load (this also validates the WebGL fix that
   already landed but has not been confirmed end-to-end).

**It passed** (2026-08-11), so the headline feature is real on Unity 6 and the marketing track is unblocked.
Had it failed, this would have become the whole project and forced item #1 (Odin removal) forward immediately —
exactly the contingency `V1_IMPROVEMENT_PLAN.md` anticipated. That contingency is now retired.

**Keep this as the release smoke.** It's the only check that covers AOT codegen + managed stripping, and no
EditMode test can replace it. Re-run before every release, and after any change that touches serialization.

---

## 2. How the two tracks interlock

> **Reordered 2026-08-11 — the breaking work now goes first.** The original version of this section put the
> launch ahead of the surgery and used the quiet 30-day measurement window to build it. That's inverted below.
> The reasoning for the change is in `done.md`; the short form is that the zero-user window is guaranteed *now*
> and only probabilistic after a launch.

The marketing plan asks for a clean 30-day measurement: ship compatibility + listing rewrite together, then
change nothing and watch daily page views. The engineering plan wants breaking surgery done while there are
zero users to migrate.

```
   ┌─ PRE-WORK ───────┐   ┌─ BREAKING WORK ───────────────┐   ┌─ LAUNCH ─┐   ┌─ MEASURE ─┐
   │ record baseline  │   │ test net (precondition)       │   │ v1.0.0   │   │ 30 days,  │
   │ Description chk  │──▶│ Odin → NarraSerializer        │──▶│ listing  │──▶│ genuinely │
   │ safe wins        │   │ Saveable + GUID identity      │   │ rewrite  │   │ quiet     │
   │ (exposure-       │   │ re-run the build smoke        │   │ video    │   │           │
   │  neutral)        │   │ (multi-window, optional)      │   │ README   │   │           │
   └──────────────────┘   └───────────────────────────────┘   └──────────┘   └───────────┘
```

**Why this order.** Both save-format changes are free only while nobody owns the plugin. Doing them *after* a
successful launch is the worst case: a save system that breaks saves in a follow-up release attacks exactly the
"will this still exist in a year?" objection the repositioning is designed to answer. Launching from a hardened
build means the first real customer never sees a migration.

**What it costs.** Roughly 12–17 evenings of delay before any market signal arrives, against a validation thesis
that says learn cheaply and early. Two things blunt it: the §1 pre-work is exposure-neutral and runs
immediately, and if the Description-field check comes back empty, the listing has never had a fair test anyway —
which raises the value of launching from a hardened build rather than re-running a broken launch.

**Two structural simplifications fall out of the reorder.** There is now **one release (1.0.0)** rather than a
1.0.0 launch followed by a 1.1.0. And the 30-day measurement window becomes *genuinely* quiet, instead of
doubling as the build window — which removes the standing risk of a mid-window ship invalidating the read.

**The zero-user window is closing on purpose.** Both breaking changes (save-format swap, `Saveable` refactor)
are free right now and expensive the moment the plugin starts selling. If marketing works, that window shuts.
So: build them during the quiet window so they are *ready to ship* the day measurement completes.

---

## 3. Track A — Marketing / validation (the critical path)

### A1. Republish as 1.0.0 with Unity 6 compatibility — *gated on Task Z*
- Bump the listing version to **1.0.0** (drop the 0.x — it's the single loudest "unfinished" signal).
- Declare 6000.x compatibility; verify the live listing's compatibility table actually shows Unity 6.
- **Success check:** the asset appears when browsing Visual Scripting filtered to 6000.x. Confirm this by hand.

### A2. Listing rewrite
- **Verify the Description / Technical Details fields are populated at all** — they rendered empty on fetch.
  Check in a logged-out browser. If they're blank, that alone explains the conversion failure and is the
  highest-value ten minutes in this entire document.
- **Reposition around save/load.** "Node graph tool" is a commodity category with a free first-party incumbent
  and Playmaker's 3,420 reviews. "Graph tool with save/restore built into the execution model" is close to a
  category of one. Lead with the pain, not the mechanism.
- Title → something searchable: `Narramancer — Node Graphs with Built-In Save/Load`.
- **Price → $39.** Expect this to raise conversions. No launch discount.
- Keywords: drop the unwinnable generics (`logic`, `Tool`, `Node`, `flow`, `visual`, `Graph`) for high-intent
  terms — `save system`, `save and load`, `game state`, `serialization`, `checkpoint`, `save anywhere`,
  `visual novel`, `dialogue`.
- Add "full source available on GitHub" as an explicit selling point — it's the strongest answer to the
  "will this be abandoned" objection, which is the load-bearing objection for a *save system*.

### A3. Demo video — **recommend pulling this forward into the launch bundle**
The handoff defers this to Priority 5 ("after traffic improves"). I'd move it into the launch bundle:

- It requires **zero new engineering** — the feature already exists, and Task Z is literally a rehearsal of the
  shot list (run → pause → save → quit → reopen → load → resume).
- The listing rewrite needs media anyway.
- It costs roughly one evening.

**Tradeoff, stated honestly:** bundling it means you can't attribute a view lift to compatibility vs. video.
But the plan already bundles A1 + A2 for the same measurement, so the read is bundled regardless — and views
are the primary metric, which the video mostly doesn't drive (it drives *conversion*). The clean-experiment
cost is small; the cost of launching a save-system listing with no proof-of-concept clip is large.

### A4. Repo as landing page
- README is currently feature documentation. Rewrite it as a landing page: demo GIF at the top, one paragraph,
  link to the store listing. Repos rank in Google for queries the Asset Store page never will.
- **Audit git history for secrets.** A filename scan across all history came back clean (no `.env`, `.pem`,
  `*key*`, `*token*`, `*credential*` ever added). Still worth a content-level pass with `gitleaks` or
  `trufflehog` before leaning on the repo as a marketing channel.
- **License decision — see §6.** This one needs your call before you promote the repo.

### A5. narramancer.com
- Move the Naninovel recommendation out of the pitch paragraph. Keep the honesty — relocate it to a comparison
  page further down the funnel. Right now the site talks the visitor out of the product at the moment of
  decision.
- Hero leads with save/load, matching the listing.
- Store link above the fold; docs cover Unity 6 setup.

### A6. Measure
Record the 3–4 views/day baseline **before** anything ships. Then 30 days, no changes, compare.
Views up → compatibility was the bottleneck. Flat → positioning problem persists, diagnose before building
more. Up but no sales → conversion problem; reviews and a free/lite tier are the next levers.

---

## 4. Track B — Engineering (B2–B4 now land *before* the launch; all of it ships as 1.0.0)

Ordered by risk, cheapest and safest first. **Nothing here ships to the store until measurement completes.**

### B1. Safe wins — ship-anytime, zero risk *(~1–2 evenings)*
These are small enough that they can go out with 1.0.0 if they're done in time, or wait.

- **`FindObjectsOfType` deprecation cleanup.** Fix the wrapper once at
  [GameObjectExtensions.cs:13](../Assets/Narramancer/Scripts/Extensions/GameObjectExtensions.cs#L13)
  (`→ FindObjectsByType<T>(FindObjectsSortMode.None)`), which covers everything routed through it, then the
  6 direct sites: `PlayAudioNode`, `PlaySoundNode`, `VerbGraphEditor` (×2), `ChoicePrinter` (×2, via the
  wrapper), `SerializableVariableReference`. **Leave `Resources.FindObjectsOfTypeAll` alone** — not deprecated.
- **#5 — TypeCache scanner.** Replace the `AppDomain` scan in `AssemblyUtilities` with
  `UnityEditor.TypeCache.GetTypesDerivedFrom` / `GetTypesWithAttribute`, per v2's `REFLECTION_UTILITIES_SPEC`.
  Immediate editor-responsiveness win with 156 node types being scanned. **Drop** the fuzzy-AQN fallback at
  [AssemblyUtilities.cs:285-299](../Assets/Narramancer/Scripts/Utilities/AssemblyUtilities.cs#L285-L299) — v2
  already retired it. Note `TypeCache` is editor-only; the runtime paths need to keep working, so this is a
  `#if UNITY_EDITOR` fast path, not a wholesale replacement.

### B2. The test net — **the precondition for everything after it** *(~3–4 evenings)*
Currently one 50-line test file. Before any surgery on the save format, lock current behavior down.

Priority order (highest value first):
1. **Save → load round-trip.** Author a story, run a verb to a suspend point, `PrepareStoryForSave()` →
   `SerializeData` → `DeserializeData` → `LoadStory`, assert identical state and that the runner resumes.
   This is the headline feature and it currently has zero coverage.
2. `Blackboard` typed set/get/remove (extend the existing file).
3. Domain ops: `NounInstance` properties/stats/relationships, bidirectional relationship integrity by UID.
4. `NodeRunner` suspend/resume.

Build on the existing `Tests.Editor` asmdef. EditMode is enough for all of the above — full PlayMode smoke can
wait for B5.

**Add a build smoke to the runbook**: Task Z (IL2CPP + WebGL round-trip) should be re-run before every release,
because it's the one thing EditMode tests structurally cannot catch.

### B3. #1 — Remove OdinSerializer *(~4–6 evenings)*
Gated behind B2. The reason this matters is concrete and commercial: **a buyer who owns Odin Inspector hits a
duplicate-assembly conflict**, because both ship OdinSerializer. That's a conversion blocker, not cleanup. It
also drops an API-compatibility-level constraint and the AOT/IL2CPP risk surface that Task Z exists to probe.

The scope is far smaller than the 186 vendored files suggest — **exactly 2 call sites**, both in
`SaveLoadUtilities`, both JSON-only:
```
SerializationUtility.SerializeValue(data, DataFormat.JSON, out wrapper.objects)     // :56
SerializationUtility.DeserializeValue<T>(bytes, DataFormat.JSON, wrapper.objects)   // :82
```
Port `NarraSerializer` from v2 (already reverse-engineered: `$type`/`$id`/`$ref`, ref-linking, Unity-object
handles, save envelope, no Reflection.Emit) behind those two calls, then delete the
`Narramancer.OdinSerializer` assembly. **This is a port, not a research project** — the hard design is done.

Ship a `schemaVersion` in the envelope from day one, and fail with a clear "incompatible save" message rather
than a crash.

### B4. Saveable refactor — one component + drivers *(~5–7 evenings)*
Full design in `SAVEABLE_REFACTOR_HANDOFF.md`. Worth doing, and worth doing **adjacent to B3** since both touch
the save format and both are free only in the zero-user window.

Two wins, one of them marketable:
- **DX:** adding a GameObject to the save system becomes "drop one component" instead of "pick the right one of
  nine `Serialize*` MonoBehaviours." Supporting a new component type becomes "write one driver." That's a
  demoable improvement and an extensibility story for third parties.
- **Robustness:** replaces the path-based key
  (`$"{field} {transform.FullPath()}[{componentIndex}]"`, [SerializableMonoBehaviour.cs:22](../Assets/Narramancer/Scripts/Components/SerializableMonoBehaviour.cs#L22))
  with a stable GUID. Today, renaming an object, reparenting it, reordering components, or having two
  same-named siblings silently breaks the save. **This is a latent correctness bug in the headline feature** —
  arguably the strongest engineering reason in this whole document.

Build order per the handoff: `Saveable` + GUID (with duplicate-paste detection — copy the approach from Unity's
open-source `GuidComponent`) → `IComponentSaver` registry + the 9 drivers → `[Save]` field scan → migrate
`RunActionVerbMonoBehaviour` / `NarramancerScene` to the guid model → spawner guid reassignment → update the 4
sample scenes.

**Keep driver `StateType`s as simple serializer-friendly structs** so they survive B3's serializer swap unchanged.

### B5. #2 — Extract `Narramancer.Core` *(~4–6 evenings)* — **optional, lowest priority**
Honest scope: a `noEngineReferences: true` netstandard2.1 assembly for the serializer, the domain model, and
utilities. It **stops at the xNode boundary** — nodes derive from xNode's `Node : ScriptableObject`, so they
cannot move. Pushing past that boundary *is* the v2 rewrite you paused.

The real payoff is a `dotnet/` mirror giving fast Unity-free CI on the extracted logic. That's genuine value,
but it's "harden for the long haul" value, and it should be gated on the market saying yes. **If the 30-day
measurement comes back flat, skip this entirely.**

### B6. #4 — Multiple xNode editor windows *(~2–3 evenings)*
Independent of everything else, self-contained (we own the vendored source), and a **real demoable feature**
for the relaunch. `V1_IMPROVEMENT_PLAN.md` left this unsized; here's the answer.

**Good news — the heavy view state is already per-window instance state:** `graph`, `panOffset`, `zoom`,
`portConnectionPoints`, `nodeSizes`, and `selectedNodeRunnerUnityObject` are all instance fields. This is a much
smaller job than feared.

**Four concrete blockers:**

1. **The actual single-window cause** — [NodeEditorWindow.cs:208](../Assets/Narramancer/Scripts/Plugins/xNode/Scripts/Editor/NodeEditorWindow.cs#L208):
   ```csharp
   NodeEditorWindow w = GetWindow(typeof(NodeEditorWindow), false, "xNode", true) as NodeEditorWindow;
   ```
   `GetWindow` always reuses the one window. Fix: search `Resources.FindObjectsOfTypeAll<NodeEditorWindow>()`
   for one already showing this graph, focus it if found, otherwise `CreateWindow`.

2. **`public static NodeEditorWindow current`** ([:11](../Assets/Narramancer/Scripts/Plugins/xNode/Scripts/Editor/NodeEditorWindow.cs#L11)),
   assigned **only in `OnFocus()`** ([:78](../Assets/Narramancer/Scripts/Plugins/xNode/Scripts/Editor/NodeEditorWindow.cs#L78)).
   70 reads across the xNode editor + 8 in Narramancer's editor code, most of them during `OnGUI` for port
   styles and coordinate conversion. An unfocused window repainting reads the *focused* window's pan/zoom.
   Fix: set `current = this` at the top of `OnGUI` (the standard xNode-fork fix), not just on focus.

3. **`VerbGraphEditor.lastOpenedStack`** ([VerbGraphEditor.cs:18-19](../Assets/Narramancer/Scripts/Editor/VerbGraphEditor.cs#L18-L19))
   — a static nav stack backed by `NarramancerSingleton.RecentlyOpenedGraphs`, plus `static bool didBack`. The
   nested-graph back button would share and corrupt history across windows. Fix: move both to per-window state.

4. **`VerbGraphEditor.selectedNodeRunner`** ([VerbGraphEditor.cs:20](../Assets/Narramancer/Scripts/Editor/VerbGraphEditor.cs#L20))
   — static, while its sibling `selectedNodeRunnerUnityObject` is already per-window
   ([NodeEditorAction.cs:49](../Assets/Narramancer/Scripts/Plugins/xNode/Scripts/Editor/NodeEditorAction.cs#L49)).
   Inconsistent; make both per-window so play-mode debugging works independently in each window.

**Known limitation to accept, not fix:** `NodeEditorBase.editors` is a static `Dictionary<target, editor>`
([NodeEditorBase.cs:19](../Assets/Narramancer/Scripts/Plugins/xNode/Scripts/Editor/NodeEditorBase.cs#L19)) with
`editor.window` reassigned on every `GetEditor` call — so opening *the same graph* in two windows is
last-caller-wins. Simplest correct behavior: focus the existing window instead of opening a duplicate (which
blocker 1's fix gives you for free). Keying the cache by `(target, window)` is the real fix if it ever matters.

---

## 5. Suggested sequence

| # | What | Track | Gate |
|---|---|---|---|
| 0 | ~~**Task Z** — IL2CPP + WebGL save/load round-trip in a built player~~ | B | ✅ **passed 2026-08-11** |
| 1 | **Record the views/day baseline** | A | do first — unrecoverable once the listing changes |
| 2 | Check whether the listing Description is populated at all | A | pure diagnostic, exposure-neutral |
| 3 | B1 safe wins (deprecations + TypeCache) | B | independent warm-up |
| 4 | **B2 test net** (save/load round-trip first) | B | **precondition for 5–6** |
| 5 | B3 Odin removal → `NarraSerializer` | B | needs B2 |
| 6 | B4 Saveable + GUID identity | B | needs B2; do near B3 |
| 7 | **Re-run the Task Z build smoke** | B | **required** — 5 and 6 changed serialization |
| 8 | B6 multi-window xNode | B | independent; optional before launch, don't let it delay 10 |
| 9 | Record demo video | A | after B4, so inspector footage doesn't go stale |
| 10 | **Republish 1.0.0** w/ Unity 6 + listing rewrite + video + README | A | **the launch** |
| 11 | ← 30-day measurement window; nothing ships → | | |
| 12 | Measure; compare to the step-1 baseline | A | 30 days after step 10 |
| 13 | B5 Core extraction | B | **only if step 12 says yes** |

Steps 1–3 run in parallel with each other and cost nothing. Step 4 is the real critical path, because both
breaking changes are gated on it. Step 7 is easy to forget and is not optional — the original Task Z pass does
not cover code that no longer exists. Step 8 is the natural evening when you don't want to think about
serialization.

---

## 6. Decisions needed from you

1. ~~**License — the one real conflict.**~~ **Resolved 2026-08-11 — relicensed to PolyForm Noncommercial
   1.0.0**, going forward only. Free to read/fork/modify and use noncommercially; commercial use requires an
   Asset Store purchase. Prior Apache-2.0 versions stay Apache-2.0 permanently (irrevocable). Vendored deps
   (MIT/Apache-2.0) are carved out — not ours to relicense. **Not OSI-approved: say "source-available," never
   "open source."** See `LICENSING.md`; review-and-commit is still open in `tasks.md` §1.

2. ~~**Is B4 (Saveable) in or out of scope?**~~ **Resolved 2026-08-11 — in, and pre-launch.** The reorder
   settled it: the path-based key is a latent correctness bug in the feature the entire pitch is built around,
   and fixing it after a launch would mean breaking a real customer's saves.

3. ~~**Demo video in the launch bundle or after?**~~ **Resolved 2026-08-11 — in the bundle, recorded after B4**
   so inspector footage shows the single `Saveable` component and doesn't need re-shooting. The
   "muddier attribution" objection largely evaporated: with one release instead of two, everything is bundled
   regardless.

4. ~~**B5 (Core extraction) — confirm it's gated on the measurement.**~~ **Resolved 2026-08-11 — yes, gated.**
   Skip entirely if the measurement comes back flat.

**No open decisions remain.**

## 7. Risks

- ~~**Task Z fails.**~~ **Retired 2026-08-11** — it passed on both Mono standalone and WebGL. This was the
  realistic bad outcome (it would have forced B3 forward as an emergency fix and pushed the launch by weeks);
  running it before committing to any marketing dates is what made it cheap to find out.
- **Scope drift back into v2.** B5 is the gateway drug — "extract the Core" becomes "extract the nodes" becomes
  the rewrite you paused. The xNode boundary is the stopping point; treat it as load-bearing.
- **The measurement window gets broken.** Shipping a fix mid-measurement destroys the read. If something must
  ship, restart the 30 days. *Much less likely after the reorder* — the window no longer doubles as the build
  window, so there's nothing scheduled inside it to leak out early.
- ~~**Breaking-change window closes.**~~ **Neutralized 2026-08-11** by moving B3/B4 ahead of the launch. This
  was the sharpest risk in the original ordering: if the marketing worked, the breaking changes acquired a
  migration burden precisely because of that success.
- **The delay costs information.** The new risk created by the reorder, and the honest counterweight to the
  item above: ~12–17 evenings pass before any market signal arrives, and the validation thesis is to learn
  cheaply and early. If the §1 diagnostics suggest the listing is fundamentally broken in some cheap-to-fix way,
  revisit whether the full §2 block really has to precede the launch.
- **§2 sprawls.** Its scope is now load-bearing on the launch date, so anything added to it directly delays
  revenue. The multi-window work is explicitly marked "don't let it delay the launch" for this reason; hold that
  line for anything else that wants in.
- **Calibration.** Per the handoff: even executed well, this plausibly lands at $100–800/month. The reason to
  do it is that it's cheap and **generates information either way** — including a real spec for v2 written by
  actual customers, which is the thing finishing v2 blind could never produce.
