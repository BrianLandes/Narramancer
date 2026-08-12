# Narramancer — brand & style guide

> **One source of truth** for color, type, motif, and voice. The editor tools, narramancer.com, and the
> Asset Store listing all reference *this* — so the product, the site, and the storefront read as one thing.
> **What the brand must convey: professional · robust · easy to use · fun to use.**

## The idea in one line
**Narramancer = *narrative* + *-mancer* (to conjure).** You conjure your game's behavior as living node
graphs, and — the superpower — you can **save the story mid-spell** and resume it exactly. The brand is
**"arcane engineering"**: the wonder of magic, delivered with the solidity of a tool you'd trust with your
save system. Deep, considered, and confident — with a vivid spark of life.

How each value shows up:
- **Professional** → a deep, rich base (not flat grey); a harmonious, disciplined palette; restraint; real screenshots.
- **Robust** → depth and structure; the node/constellation motif; weight; "the save system you can trust."
- **Easy** → high contrast, generous whitespace, plain-language copy, simple icons.
- **Fun** → the vivid accent *spark*, a touch of the arcane, energy and motion (a running graph *pulses*).

## Color

The palette **evolves the colors already in the graph editor** into a documented system. Base = deep
navy-teal (robust, premium). Accents = a cohesive, vivid, accessible set (fun, clear). **Discipline is the
whole game: 60% base / 30% surfaces & muted / 10% accent.** One signature color does the pointing.

### Neutrals — the "Ink" family (deep navy-teal, not grey)
| Token | Hex | Use |
|---|---|---|
| `ink-void` | `#050A0E` | deepest background (evolves the editor's `#030C11`) |
| `ink-900` | `#0A1620` | primary dark surface |
| `ink-800` | `#0F2130` | raised panels / cards (near the editor's `#001829`) |
| `ink-700` | `#17324A` | borders / dividers on dark |
| `slate-500` | `#63768D` | muted text, secondary UI (already in the editor) |
| `mist-300` | `#AEBECF` | secondary text on dark |
| `paper-50` | `#F3F6F9` | primary text on dark; background in light contexts |

### Accents — vivid, cohesive, purposeful
| Token | Hex | Role |
|---|---|---|
| **`spark-500`** | **`#EF476F`** | **THE signature.** CTAs, the logo spark, the "running/alive" state. (Already the editor's running-node highlight.) Use it where you want the eye to go — sparingly. |
| `gold-400` | `#FFD166` | warmth, value, highlights (cleans up the editor's `#F7EE7F`) |
| `flow-400` | `#06D6A0` | the **save/flow/success** color — teal-green, ties to the base; use for the save-system story |
| `azure-500` | `#118AB2` | links, structure, informational |
| `arcane-500` | `#9B5DE5` | the **magic** flavor — nouns/entities, occasional narrative accent. Use with restraint. |

> These accents (`#EF476F`/`#FFD166`/`#06D6A0`/`#118AB2` on a deep teal-navy) are a proven, harmonious set —
> vivid enough to feel *fun*, balanced enough to stay *professional*.

### Semantic — node-graph type colors (consistency across editor + docs + screenshots)
Reuse the accents as the data-type language so a graph screenshot on the store matches the live editor:
`string → slate #63768D` · `int/number → gold #FFD166` · `float → azure #118AB2` · `bool → flow #06D6A0` ·
`noun → arcane #9B5DE5` · **flow/exec wire → spark #EF476F (running) / rose #A54657 (idle)** (the editor's
existing `#A54657`).

### Rules
- **60 / 30 / 10.** Deep base dominates; accents are seasoning. Rainbow = "cheap." Restraint = "professional."
- **One signature** — `spark-500` is the brand's pointing finger (primary button, key highlight, the mark).
  Everything else supports.
- **Contrast is non-negotiable** — body text is `paper-50`/`mist-300` on `ink-*`, meeting WCAG AA. Never
  slate-on-ink for anything you must read.
- **Accent on accent = no.** Vivid colors sit on ink, not on each other.

## Typography
- **UI + web body:** a clean geometric-humanist sans — **Inter** (or Figtree/Manrope). Free, modern, neutral-professional, great on screen.
- **Headlines / wordmark:** a face with a little character but still disciplined — a tight geometric or a
  subtly arcane display for the logo; headlines can be the same sans in heavy weight. Avoid novelty "fantasy" fonts.
- **Code / node-type / technical:** a monospace — **JetBrains Mono** or **IBM Plex Mono** — for the
  engineering credibility.
- Keep to **two families + mono.** Big, confident headings; generous line-height on body; left-aligned.

## Motif & shapes
- **The node-constellation.** Connected dots-and-lines (nodes + wires) are the brand's visual DNA — as a
  logo mark, section dividers, background texture (faint), and iconography. One node **glows spark-pink** =
  "alive / running."
- **Rounded-rectangle nodes, clean wires**, subtle depth (soft shadows on ink) — structured but friendly.
- **Motion = life.** Where motion is possible (web, video), a running graph *pulses* along its wires in
  spark-pink. This single idea sells "living, resumable behavior" better than any bullet list.
- **Avoid the dev-tool clichés:** gears, brackets `</>`, puzzle pieces, generic circuit boards. They read cheap.

## Logo (✅ chosen: the node-sigil "N")
**Direction A — the node-sigil "N".** Four connected nodes form the strokes of an "N"; the corner nodes are
neutral (mist), and **the top-left node is "alive"** — spark-pink with a soft glow. It says *node graph*
(robust) and *the running/living spell* (fun/magic) in one mark, and it's the origin of the stroke, so the
eye starts there. Pair with the **"Narramancer"** wordmark (Narra in paper, mancer in slate).
- **Alive node = top-left**, always. That single glowing node is the brand's recurring tell (it reappears in
  the top-left of every object icon — see below).
- **App/store icon:** the "N" mark alone in a rounded-square Ink tile (the same frame as the object icons).
- Retired: the illustrative spellbook icon (off-brand vs the node system; reads clip-arty).

## Object icon system (the bespoke ScriptableObjects)
Every Narramancer asset icon is built from **one recipe** so the set reads as a family in the Project window,
while each type reads as itself. Replaces the current mismatched set (an illustrative spellbook + stark B&W
glyphs). Every icon = three ingredients:
1. **Ink frame** — a rounded-square node tile (`ink-800 #0F2130` on a `ink-700 #17324A` border). The shared body.
2. **The brand tell** — a small glowing node in the **top-left** corner (the logo's alive node), in the
   type's accent. Same corner, every icon — the thread that ties the set to the mark.
3. **Accent glyph** — a mark in **node-graph vernacular** (nodes, wires, a shape), in the type's accent color.

**Glyph = the type · color = its role:**
| Asset | Accent | Glyph |
|---|---|---|
| **Noun** | arcane `#9B5DE5` | a solid node with an orbit ring — the entity everything attaches to |
| **Action Verb** | spark `#EF476F` | a node with a ▶ play mark + a flow arrow — behavior that *runs* |
| **Value Verb** | azure `#118AB2` | a node with a return arrow — hands a value back, instantly |
| **Property** | gold `#FFD166` | a tag — a quality a noun has or hasn't |
| **Stat** | flow `#06D6A0` | rising bars — a number that changes |
| **Relationship** | arcane-lite `#C77DFF` | two nodes + a directional arrow (A → B) |
| **Flag** | slate `#63768D` | a pennant — a global state marker |

- **Legibility:** glyphs are simple enough to read at ~20px (the Project-window size). Test every icon there.
- **Consistency = the frame + the top-left node.** Distinctiveness = the glyph. Never break the frame to be clever.

**Coverage (v1 project scan):**
- ✅ *Done* — the 7 above + **RankedWeightedAction** (gold; a source node fanning weighted arrows) + the
  **app/package icon** = the node-sigil "N" in the Ink tile (replaces the old spellbook; wired to
  `NarramancerSingleton`).
- *No icon needed* — abstract/runtime types that aren't createable assets (`AdjectiveInstance`, `VerbGraph`,
  `AdjectiveScriptableObject`, `Singleton` bases). `AbstractIngredient` only if ingredients stay user-facing.
- *Next (optional polish)* — **component icons** for the menu-registered MonoBehaviours (`NarramancerScene`,
  `RunActionVerbMonoBehaviour`, `SerializableSpawner`, `CreateNounForGameObject`,
  `SetGlobalVariablesMonoBehaviour`, and the future `Saveable`): same Ink-tile frame, a node-graph glyph.
  Sample-only UI components (printers, save/load menus) are lowest priority.

## Applying it per layer
- **Editor tools:** already the closest — formalize `ink-*` for chrome, the semantic type colors for ports,
  `spark-500` for running/selected. Pull hex values from this doc (later: a shared `TypeColorService`-style
  source so nothing hardcodes stray greys). Kill the ad-hoc `new Color(0.2f,0.2f,0.2f)` greys → `ink-*`.
- **Website (fix "walls of text, boring"):** lead with a **hero** — the save-mid-execution promise + the demo
  GIF — on an `ink-void` background with a `spark-500` CTA. Break every wall of text into scannable sections
  with the node motif, icons, and short punchy copy. Gold/teal/azure to differentiate sections. Move the
  Naninovel comparison off the decision moment (per the validation handoff).
- **Store listing (fix "cheap"):** one consistent template across icon, card, and screenshots — `ink` bg,
  the constellation motif, a bold headline leading with **save/load**, one accent, and **real editor
  screenshots** (which already use the palette). Hero image = a graph paused mid-run (pink highlight) → save →
  resume. Consistency is what reads as "professional."

## Voice & tone
Confident, clear, a little playful-arcane — never cutesy, never jargon-walls. Lead with the player/dev's
problem in plain words, then the "spell." Examples: *"Conjure your game's behavior as node graphs."* ·
*"Save the story mid-spell — and pick up exactly where you left off."* · *"A save system you don't have to
build."* Professional enough to trust with a save system; fun enough to enjoy using.

## Quick reference (design tokens)
```
ink-void #050A0E · ink-900 #0A1620 · ink-800 #0F2130 · ink-700 #17324A
slate-500 #63768D · mist-300 #AEBECF · paper-50 #F3F6F9
spark-500 #EF476F  ·  gold-400 #FFD166  ·  flow-400 #06D6A0  ·  azure-500 #118AB2  ·  arcane-500 #9B5DE5
signature = spark-500 · ratio 60/30/10 · fonts: Inter + display + JetBrains Mono
```
