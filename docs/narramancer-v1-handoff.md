# Narramancer v1 — Validation Handoff

**Goal:** Determine whether Narramancer can sell, at the lowest possible cost, before investing further in the v2 rewrite.

**Status:** v2 rewrite (Unity Graph Toolkit) is **paused at phase 2 of ~8** pending the outcome of this work.

---

## 1. Context

**What Narramancer is:** A Unity plugin. Node-based scripting system where saving and loading game state are built in. Used for branching conversations, NPC behavior trees, or overall game-loop logic. The core value: you can save and restore a *running* sequence at almost any point, rather than bolting a save system on later.

**Asset Store listing:** `assetstore.unity.com/packages/tools/visual-scripting/narramancer-269301`
**Docs site:** narramancer.com
**Publisher:** Professional Bad Guys (publisher ID 50556)
**Source repo:** recently made public

### Current listing state (as observed)

| Field | Value | Problem |
|---|---|---|
| Version | 0.5.7 | Sub-1.0 reads as "unfinished / beta" |
| Last release | Apr 4, 2024 | 2+ years stale; reads as abandoned |
| Unity compatibility | **2021.3.25f1 only** | Invisible to anyone filtering for Unity 6 |
| Price | $7.99 | Below the cheapest competitor; signals hobby project |
| Ratings | None | No social proof |
| Favorites | 22 | **People find it and like the idea** |
| Traffic | 3–4 views/day | Effectively invisible |
| Sales | ~0 lifetime | — |
| File size | 3.4 MB | Fine |

### The diagnosis

**The product has not been rejected by the market, because the market has not seen it.**

The 22 favorites against ~0 sales is the key signal: people who find the listing understand the concept well enough to bookmark it, then don't buy. That's a trust and conversion failure, not a demand failure. The concept lands; the listing doesn't close.

Three causes, in order of likely impact:

1. **Version invisibility.** Unity 6 (6000.3 LTS / 6000.4 mainline) is the current line. The Asset Store sidebar has version filters — anyone browsing with 6000.x selected never sees this asset. Anyone who does see it reads "2021.3" and leaves.
2. **Abandonment signals.** Version 0.5.7 + last updated April 2024. For a *save system* — the most load-bearing dependency in a project — the buyer's real question is "will this exist in a year," and the listing answers "no."
3. **Price as a negative signal.** In developer tools, price signals quality and longevity. $7.99 reads as abandonware.

---

## 2. Competitive landscape

Category: Tools → Visual Scripting (223 assets). Listing also appears under Behavior AI.

| Asset | Price | Ratings |
|---|---|---|
| Playmaker | $65 | 3,420 |
| Behavior Designer | $95 | 762 |
| NodeCanvas | $120 | 369 |
| FlowCanvas | $120 | 162 |
| GameFlow | Free | 164 |
| Arbor 3 | $80 | 29 |
| xNode | $10 | 28 |
| Unity Visual Scripting | Free (in engine) | — |

**Strategic read:** Visual Scripting is unwinnable as a browse category — a free first-party incumbent ships in the engine, and Playmaker has 3,420 reviews. Narramancer cannot compete as "another node tool."

**The wedge:** save/load. Arbitrary mid-execution state serialization in Unity is a genuinely painful, widely-complained-about, poorly-solved problem. Devs hack it with JSON dumps and scene reloads and hate it. "Node graph tool" is a commodity; "graph tool with save/restore built into the execution model" is close to a category of one.

**Reposition the entire pitch around save/load as the headline, with node graphs as the mechanism.**

---

## 3. Work plan

### Priority 1 — Unity 6 compatibility (BLOCKING)

Nothing else matters until this is done.

- [ ] Open v1 in Unity 6 LTS (6000.3). Build. Catalog what breaks.
- [ ] **Decision gate:**
  - **Runs or is patchable** → fix, republish. v2 stays paused.
  - **Fundamentally broken** (depends on APIs that no longer exist) → v2 is forced; scope to the smallest shippable subset rather than all 8 phases.
- [ ] Republish with 6000.x compatibility declared
- [ ] Version it **1.0.0** — drop the 0.x
- [ ] Verify the compatibility table on the live listing shows Unity 6

**Success check:** listing appears when browsing Visual Scripting filtered to Unity 6000.x.

### Priority 2 — Store listing rewrite

- [ ] **Verify the Description and Technical Details fields are actually populated.** On fetch they rendered empty. Check in a logged-out browser. If blank or thin, this alone explains everything.
- [ ] **Title:** currently just "Narramancer" — a coined word matching no search anyone types. If the field allows, something like `Narramancer — Node Graphs with Built-In Save/Load`.
- [ ] **Price → $39.** Expect this to *increase* conversions. Do not discount at launch; the $7.99 sale price is part of the problem.
- [ ] **Description structure:**
  1. Lead with the save/load problem in the buyer's words ("Adding save/load to a Unity game after the fact is miserable...")
  2. The one-line answer (save and restore the running graph at any point)
  3. Use cases: visual novels, branching dialogue, NPC behavior, game-loop logic
  4. What's included, requirements, support links
- [ ] **Keywords.** Current tags are unwinnable generics: `logic`, `Tool`, `Node`, `flow`, `visual`, `Graph`. Replace the weak ones with high-intent, low-competition terms: `save system`, `save and load`, `game state`, `serialization`, `checkpoint`, `save anywhere`, `visual novel`, `dialogue`.
- [ ] **Category.** Confirm which of Visual Scripting / Behavior AI is primary. Consider whether a less-contested category serves better.
- [ ] Consider renaming the publisher from "Professional Bad Guys" — it signals nothing relevant to someone evaluating a save system. Low priority.

### Priority 3 — Repo

Repo stays **public**. It is the strongest available answer to the "will this be abandoned" objection, and it's a discovery channel that currently doesn't exist. Precedent: xNode is open source and sells on the Asset Store.

- [ ] **Audit git history for secrets** — keys, tokens, personal paths. History retains deleted files; check commits, not just the working tree.
- [ ] **License check:**
  - No LICENSE file → default all-rights-reserved. Fine, but ambiguous. Add something explicit.
  - MIT / Apache → reconsider. Can be changed going forward, but published snapshots stay under the old terms. Low practical risk at current traffic.
- [ ] **Target license:** source-available, not permissive. "Read, fork, evaluate freely; commercial use in a shipped product requires an Asset Store license." PolyForm Noncommercial is an off-the-shelf option.
- [ ] **README as a landing page:** save/load demo GIF at the top, one-paragraph description, link to the store listing. Repos rank in Google for queries the Asset Store page never will.
- [ ] Add the public repo as a *selling point* on the store page — "full source available, you're never stranded."

### Priority 4 — narramancer.com

- [ ] **Move the Naninovel recommendation.** The site currently tells visual-novel developers who prefer text-based scripting to use Naninovel instead, describing it as feature-rich with years of experience and support — inside the pitch paragraph, at the exact moment of decision. Keep the honesty; relocate it to a separate comparison page further down the funnel.
- [ ] Hero section leads with save/load, matching the store listing.
- [ ] Verify docs cover Unity 6 setup once compatibility ships.
- [ ] Ensure the store link is prominent and above the fold.

### Priority 5 — Video demo (after traffic improves)

The single highest-conversion asset available. 60–90 seconds, no narration required:

> Run a graph → pause mid-execution → save → quit the editor entirely → reopen → load → resume exactly where it was.

That clip is worth more than every feature bullet combined. Use it on the store page, the README, narramancer.com, and as the r/Unity3D / Unity Discussions post.

---

## 4. Measurement

**Primary metric: daily page views.** Not sales — sales lag reviews, and there are no reviews yet.

Baseline: 3–4 views/day.

- [ ] Record baseline before changes
- [ ] Ship Priority 1 + 2 together
- [ ] Wait **30 days**, no further changes
- [ ] Compare

**Interpretation:**
- Views up substantially → compatibility was the bottleneck. Proceed to video + keyword pass.
- Views flat → discovery/positioning problem persists. Diagnose before building more.
- Views up, sales still zero → conversion problem. Video and reviews are the next lever; consider a free/lite tier to seed reviews.

---

## 5. Calibration

Even executed well, a tool from an unknown publisher in a crowded category more plausibly lands at **$100–800/month** than anything transformative. That's a real and worthwhile number — but this is the "meaningful side income" bucket, not the "this is the one" bucket.

The reason to do this work is that it's **cheap and generates information either way.** If it sells, it validates the product *and* produces actual customers whose feedback becomes a far better spec for v2 than the current one. If it doesn't sell after a real marketing effort, that's learned in three weekends instead of six months.

Finishing v2 blind generates no information until the very end.

---

## 6. Open questions

- Does v1 build and run on Unity 6 LTS without major rework?
- Are the Description / Technical Details fields on the listing actually populated?
- What license (if any) is currently on the public repo?
- Do publisher dashboard numbers confirm the ~3–4 views/day and show a referrer breakdown?

---

## 7. Portfolio context

Sparclean BNB is the #1 priority project and gets daytime hours (customer outreach is calendar-bound). Narramancer gets evening hours — but only the validation work above, not v2 phases 3–8. GobbleGo is in maintenance mode with API spend capped. Alsion stays alive but gets no new development. Contract work (Flutter / Supabase / Cloudflare) is the near-term cash bridge.
