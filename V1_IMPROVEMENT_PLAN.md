# Narramancer v1 — engineering improvement plan

> **Pivot:** v2 (Graph Toolkit) is paused; harden **v1** and get it selling. This plans the *engineering*
> track (the marketing track lives in the validation handoff). **Guiding rule: the sellable build stays green
> the whole time** — Unity 6 compatibility ships first, and nothing here is allowed to destabilize the
> shippable plugin or delay the marketing launch. **The paused v2 is not wasted: it is the R&D kit that
> de-risks four of these five items** (we already built the serializer, the Core-split pattern, the test
> harness, and the TypeCache optimization). Evenings-only scope; "meaningful side income," not "the one."

## Two facts that shape the whole plan
1. **"Odin" here = the free, open-source *OdinSerializer*, vendored** in `Assets/Narramancer/Plugins/OdinSerializer/`
   — **not** paid Odin Inspector. The motivation to remove it is concrete: **a buyer who owns Odin Inspector
   hits a duplicate-assembly (DLL) conflict** because both ship OdinSerializer; it also forces an API
   compatibility level and adds AOT/IL2CPP risk. So removal is a *conversion-blocker fix*, not cosmetics.
2. **xNode is vendored source** (modifiable → #4 is feasible), but **nodes derive from xNode's
   `Node : ScriptableObject` (UnityEngine).** This is a **hard ceiling on #2**: nodes can't move to an
   engine-agnostic Core while we stay on xNode. Full engine-agnostic Core = replacing xNode = drifting back
   into a v2-style rewrite, which we're explicitly *not* doing. #2 is therefore a **partial** extraction.

## Current assembly layout (starting point)
`Narramancer.asmdef` (runtime, `noEngineReferences:false`, → xNode) · `Narramancer.Editor.asmdef` ·
`Narramancer.OdinSerializer.asmdef` (vendored) · xNode runtime + `XNodeEditor.asmdef` ·
`Tests.Editor.asmdef` (one `BlackboardTests.cs` — a harness skeleton to build on).

## What we harvest from v2 (the "what we learned" ledger)
| v1 item | Harvest from v2 | Head start |
|---|---|---|
| #1 remove Odin | **`NarraSerializer`** (already reverse-engineered Odin's features: `$type`/`$id`/`$ref`, ref-linking, Unity-object handles, save envelope, **no Reflection.Emit**) | The hard design is **done** — this becomes a port, not a research project |
| #2 Core assembly | v2's **Core split + `dotnet/` mirror** pattern (netstandard2.1, `noEngineReferences`, CI) | Proven blueprint + the exact asmdef/CI recipe |
| #3 tests | v2's **`TESTING_STRATEGY.md`** + the `dotnet` NUnit suite model | Layer plan + which tests live where |
| #5 assembly scanner | **`REFLECTION_UTILITIES_SPEC.md`** (`UnityEditor.TypeCache` replaces AppDomain scans) | Drop-in optimization, already specced |
| #4 multi-window xNode | *(net-new — v2 used GTK)* | The multi-graph *need* is itself a GTK-vs-xNode lesson |

---

## Sequencing — risk-managed, marketing-first

### Phase 0 — Unity 6 compatibility (BLOCKING GATE)
Nothing sells until this ships. *(From the handoff; precondition to all of the below.)*
- [ ] Open v1 in Unity 6 LTS (6000.x); compile; **catalog what breaks** (Odin, xNode, our code).
- [ ] Fix minimally to compile + run the example scenes. **Decision gate:** patchable → fix + republish as
  **1.0.0**; fundamentally broken → escalate (may force parts of #1 early — see contingency).
- [ ] Verify save/load round-trips on Unity 6 (the headline feature — smoke it by hand now, test it in #3).
- **Contingency:** if the vendored OdinSerializer is what breaks on Unity 6, #1 gets pulled forward as part
  of the compat fix rather than a later phase.

### Phase 1 — Safe wins + the safety net (parallel to the marketing push; zero risk to the sellable build)
- [ ] **#5 — Assembly-scanner optimization.** Swap `AssemblyUtilities` AppDomain scans for
  `UnityEditor.TypeCache` (`GetTypesDerivedFrom`/`GetTypesWithAttribute`) per `REFLECTION_UTILITIES_SPEC`.
  Small, isolated, immediate editor-speed win; **drop** the field-scan reverse-ref + fuzzy-AQN paths that v2
  already retired. Good warm-up task.
- [ ] **#3 (start) — Characterization / regression net.** Before *any* risky refactor, lock current behavior
  under test — most importantly **save→load round-trip** (author a story, run a verb to a suspend point, save,
  reload, resume; assert identical state) and the blackboard/domain. Build on the existing `Tests.Editor`
  asmdef. This net is the precondition for #1 and #2, and it directly protects the feature you're about to
  market.

### Phase 2 — The big harvest (gated behind the Phase-1 net; the zero-user window is the cheapest time)
> **Key timing insight:** with ~0 sales there is **no installed save-base to keep compatible** — so the
> serializer swap is *free of migration burden now* and becomes a breaking change with real cost the moment
> the plugin starts selling. Do it early, behind the net.
- [ ] **#1 — Remove OdinSerializer, adopt `NarraSerializer`.** Port v2's serializer; replace OdinSerializer at
  every save/load site; delete the vendored `Narramancer.OdinSerializer` assembly. Payoffs: **kills the Odin
  Inspector DLL conflict**, removes the API-compat-level constraint, reduces AOT/IL2CPP risk, drops a
  dependency. Guarded by the #3 round-trip tests every step.
- [ ] **#2 — Extract `Narramancer.Core` (partial, honest scope).** Split a `noEngineReferences:true`
  netstandard2.1 assembly for the **cleanly-extractable** logic: the serializer, the domain model
  (nouns/adjectives/stats/relationships/blackboard/save-state), and utilities. **Leave** on the Unity side:
  xNode nodes, MonoBehaviours, editors. Mirror v2's `dotnet/` project so extracted Core gets **fast,
  Unity-free CI** — a green `dotnet` build doubles as a Unity-compat guarantee. **Stop at the xNode boundary**
  (don't chase full node extraction — that's v2's job, and it's paused).
- [ ] **#3 (deepen) — Full coverage as seams open.** Extracted Core → fast `dotnet` unit/integration tests
  (the high-value layer). Keep EditMode for Unity-coupled logic + adapters; a few PlayMode smokes for the
  full author→run→save→reload→resume path. Don't mirror Core coverage in Unity (per `TESTING_STRATEGY`).

### Phase 3 — Feature improvement (anytime; independent; marketing-visible)
- [ ] **#4 — Multiple xNode editor windows.** Modify the vendored xNode editor so several graph windows can be
  open on different graphs at once (today it's single-window/single-graph). Self-contained (we own the
  source), independent of #1/#2, and a **real, demoable feature** to tout in the relaunch. Watch for xNode's
  static/singleton editor state — the core of the work is making window↔graph state per-window instead of
  global.

## Dependency graph (why this order)
```
Phase 0  Unity 6 compat ──────────────► (unblocks selling; gates everything)
Phase 1  #5 scanner (isolated) ────────► ship anytime
         #3 net (round-trip tests) ────► REQUIRED before #1/#2
Phase 2  #1 Odin→NarraSerializer ──────► needs #3 net; do in zero-user window
         #2 Core extraction ───────────► needs #3 net; eased by #1; capped by xNode
         #3 deepen ────────────────────► follows #2 seams
Phase 3  #4 multi-window xNode ────────► independent; any time
```

## Honest ceilings & cautions
- **#2 is partial by design.** You get a Core of serializer + domain + utils, not v2's full engine-agnostic
  split — because xNode nodes are ScriptableObjects. That's the correct stopping point; pushing past it *is*
  the v2 rewrite you paused.
- **Protect the sellable build.** Phase 2 is real surgery. Keep it on a branch, gated by the #3 net, and never
  let `main`/the shippable build go red during a marketing window. If validation is weak, Phase 2 can pause
  without losing the Phase-0/1 gains.
- **Don't over-invest ahead of signal.** The handoff's whole thesis is *validate cheaply first*. Phase 0 + the
  cheap Phase-1 wins get you selling; Phase 2's value is real but is "harden for the long haul," appropriately
  gated on the market saying yes (and on evening hours).

## Open questions
- Does v1 compile/run on Unity 6 as-is, or does OdinSerializer/xNode force fixes (→ may reorder #1)?
- Is the vendored OdinSerializer version recent enough for Unity 6, or would an upgrade be as much work as removal?
- How much xNode editor state is global vs per-window (sizes #4)?
- Save-format change (#1): confirm ~0 installed base so we can change it freely now — verify against publisher
  dashboard before assuming no migration is needed.
