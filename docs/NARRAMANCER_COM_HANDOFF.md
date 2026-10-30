# Handoff: rework narramancer.com for the 1.0.0 relaunch

**For:** a fresh session working in the **narramancer.com site repo**, possibly on another machine. It doesn't
need this plugin repo checked out: everything it needs is linked below as a GitHub URL. Those links work once
`main` is pushed to `github.com/BrianLandes/Narramancer`.

**Written:** 2026-10-29. **Owner:** Brian.

---

## Why this is happening

Narramancer is a Unity Asset Store plugin: node graphs for story, dialogue and game logic, where **saving and
loading a running graph is built into the execution model**. Save mid-scene, quit, reload, and every graph
resumes on the exact node it was on. That's the product. The node graphs are how it works.

Version **1.0.0** is a relaunch, and everything in it ships together:

- a new package version (Unity 6, a new serializer, a new way to save scene objects),
- a rewritten Asset Store listing: title **"Narramancer: Node Graphs with Built-In Save/Load"**, $39,
- a 79-second demo video,
- a GitHub README rewritten as a landing page,
- **this site**.

The site, the listing and the README have to tell the same story on the same day. The listing copy is the
reference to match: [`docs/LAUNCH_COPY.md`][copy].

**Timing:** do the work now, but **don't deploy until the Asset Store approves 1.0.0.** Before that the store
still sells 0.5.7, and the site would describe a version nobody can buy. There's also a 30-day measurement
window after launch, during which nothing ships, so get the site right in one go.

---

## What the site looks like now

As fetched on 2026-10-29. Re-check this in the repo before relying on it.

- **Pages:** Introduction (`/`), Getting Started, Verbs, Verb Editor, Nodes, List of All Nodes, Nouns, Narramancer
  Window, Saving and Loading, Components, Flags. Each has a "Next" link at the bottom.
- **Hero:** "Narramancer is a node-based scripting system where saving and loading the game state are built-in."
  Then sections for Overview, Background, Why Narramancer? and Advantages.
- **"Why Narramancer?"** is a comparison table covering plain Unity, Unity Visual Scripting, Fungus and
  **Naninovel**. The Naninovel row says *"Naninovel is a powerful visual novel engine that does have saving and
  loading built-in"* and recommends it to visual-novel developers who prefer text scripting.
- **Store link:** an "Asset Store" link in the top bar, pointing at
  `https://assetstore.unity.com/packages/tools/behavior-ai/narramancer-269301`. The listing now lives under
  **Tools > Visual Scripting**.
- **Unity version:** not mentioned anywhere.
- **Stack:** couldn't be told from outside. Find out from the repo first.

---

## What to change

### 1. The home page leads with save/load

- **Hero:** lead with the problem and the promise, matching the listing. Suggested:
  - Headline: **Save anywhere. Even mid-scene.**
  - Sub: *Node graphs for Unity 6 where saving and loading are built in. Save in the middle of a conversation,
    a walk or a timer, quit, reload, and every graph picks up on the exact node it was on.*
  - Primary button: **Get it on the Asset Store** (accent colour). Secondary: **Watch the demo**.
- **The store link goes above the fold**, as a real button in the hero, not only in the top bar.
- **Put the demo under the hero.** Use a YouTube embed, or the looping GIF/WebM linked to YouTube (see Media).
- **Then a "What gets saved" section**, the same two-column split as the README: what's saved automatically,
  and what's saved where you add a `Saveable`.
- Keep "Background" and "Advantages", but trim them and move them below the save/load story.

### 2. Move the Naninovel recommendation down the funnel

It currently talks visual-novel developers out of the product inside the pitch, at the moment they're deciding.
**Keep the honesty, move the location.** Make a **Comparison** page (e.g. `/comparison`) with the full table,
Naninovel's row included, and link it from the bottom of the home page ("How does it compare?"). Remove the
comparison table from the home page, or cut it to one line per tool with no recommendation to go elsewhere.

### 3. Fix the store link everywhere

Use `https://assetstore.unity.com/packages/tools/visual-scripting/narramancer-269301`. The id is what matters,
but use the current category path.

### 4. The docs describe 1.0, not 0.5

The **source of truth is the shipped Getting Started guide**: [`docs/getting-started/guide.html`][guide], which
is built into the PDF that ships in the package. The site must not contradict it. Lift wording from it freely.

| Site page | What to do | Guide chapter |
|---|---|---|
| Getting Started | Rewrite: **Unity 6**, install from My Assets, open Window → Narramancer once. **Delete the ".NET 4.x API compatibility" step**; it's no longer needed. Add the samples table, including **0. Sample Launcher**. | 2, 3 |
| Saving and Loading | **Rewrite.** One component, `Saveable`, decides what a scene object saves. Cover the `[Save]` attribute, the asset manifest and "Additional Assets", where save files go (editor, build, WebGL), and "test saves in a build". | 6 |
| Components | Remove every `Serialize…` component except `SerializeNounInstanceReference`, which is unchanged. Add `Saveable`. Check the rest against what's under `GameObject → Narramancer`. | 6, 7 |
| **Upgrading from 0.5** (new) | 0.5 saves can't be loaded; Odin is gone; the mapping from `Serialize…` components to `Saveable` toggles; Add Missing Saveables. | 7 |
| Narramancer Window | Check against the guide: editor tabs are Nouns, Adjectives, Verbs, Variables; Play Mode shows the live noun table. | 5 |
| Verbs, Nouns, Flags, Verb Editor | Mostly still right. Skim for Odin, ".NET 4.x" and Unity 2021 mentions. The graph editor has a new **toolbar** (Back, Recent, Find Verb, Add Node, Frame, Collapse, New Child Verb, More); screenshots of the old header are stale. | 4 |
| List of All Nodes | The Inequality node's options were relabelled: the old "Greater Than" and "Less Than" are now **Greater Than Or Equal To** and **Less Than Or Equal To**, with strict **Greater Than** and **Less Than** added. If the list was generated, regenerate it. Otherwise, fix that entry. | — |

Mention **Unity 6** wherever setup is described.

### 5. A footer licence line

Add: *Narramancer is source-available. It's free for noncommercial use under PolyForm Noncommercial 1.0.0, and
commercial use needs an Asset Store license.* Link it to [`LICENSING.md`][licensing] and the GitHub repo.

---

## Rules (these are not style suggestions)

- **"Source-available" or "full source included", never "open source".** PolyForm Noncommercial isn't
  OSI-approved, and developer communities police the difference hard.
- **Don't claim more than has been tested.** Save/load is verified in WebGL (IL2CPP) player builds, on the
  Built-in render pipeline. Don't promise mobile, consoles, URP or HDRP.
- **Odin Inspector:** say "Narramancer no longer ships a copy of Odin Serializer". **Don't** say "works alongside
  Odin Inspector" until the coexistence check in the plugin's `tasks.md` is ticked.
- **No `Narra*` names.** Types are `NarramancerSingleton`, `Saveable`, `NodeRunner` and so on.
- **Brand:** the full guide is [`docs/BRAND_STYLE_GUIDE.md`][brand]. The shipped UI palette, which the site should
  match: backdrop `#0B1A26`, navy `#0F2130`, surface `#162232`, raised `#1E2E42`, hover `#2A3F58`, accent
  `#EF476F` (pressed `#C93A5C`), ink `#E4EBF2`, slate `#AEBECF`. Use the accent sparingly: primary buttons and
  the "alive" node. The video and store images use Open Sans; the brand guide prefers Inter for the web. Either
  is fine, but use one throughout.

---

## Media

All of this is in the plugin repo under [`docs/media/`][media]. Copy files into the site repo; don't hot-link
`raw.githubusercontent.com`.

| File | Use |
|---|---|
| `narramancer-save-load-loop.webm` (1 MB) | Home page hero loop: `<video autoplay muted loop playsinline>` |
| `narramancer-save-load-loop.gif` (5.8 MB) | Fallback for the loop |
| `still-1-verb-running.png` … `still-5-saveable.png` (1920×1080) | Section images, Open Graph image |
| `store/social-1200x630.png` | `og:image` / `twitter:image` for the home page |
| `store/icon-160x160.png` | Favicon source and header mark |
| The demo video on YouTube | Embed on the home page. **Brian has the link.** It's Unlisted until launch day. |

---

## Done when

- [ ] Home hero leads with save/load; Asset Store button above the fold; demo visible without scrolling far
- [ ] Naninovel comparison moved to its own page, linked from the bottom of the home page
- [ ] Every store link uses the `/visual-scripting/` URL
- [ ] Getting Started, Saving and Loading, and Components describe 1.0; a new Upgrading from 0.5 page
- [ ] No mention of Odin as a requirement, ".NET 4.x", or Unity 2021 anywhere (grep for it)
- [ ] "Open source" appears nowhere (grep for it)
- [ ] `og:image` set; the page looks right on a phone
- [ ] Deployed on the day the Asset Store approves 1.0.0, not before

[copy]: https://github.com/BrianLandes/Narramancer/blob/main/docs/LAUNCH_COPY.md
[guide]: https://github.com/BrianLandes/Narramancer/blob/main/docs/getting-started/guide.html
[licensing]: https://github.com/BrianLandes/Narramancer/blob/main/LICENSING.md
[brand]: https://github.com/BrianLandes/Narramancer/blob/main/docs/BRAND_STYLE_GUIDE.md
[media]: https://github.com/BrianLandes/Narramancer/tree/main/docs/media
