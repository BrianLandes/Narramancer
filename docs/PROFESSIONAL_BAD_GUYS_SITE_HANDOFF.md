# Handoff: a new website and brand outline for Professional Bad Guys

**For:** a fresh session building a new site for **Professional Bad Guys** in its own repo, probably on another
machine. This file stands on its own. Links into the Narramancer repo are GitHub URLs, and they work once that
repo's `main` is pushed.

**Written:** 2026-10-30. **Owner:** Brian Landes.

**Scope:** an **outline** of the brand and styling guide, plus the site it drives. Decide the open questions
with Brian first, then fill the outline in as the brand guide, then build the site from it. Don't skip straight
to pixels.

---

## 1. What Professional Bad Guys is, today

- **The publisher name behind Narramancer.** It's the Unity Asset Store publisher (publisher ID 50556), the
  copyright holder in Narramancer's `LICENSE`, and the company name in its Unity project.
- **A one-person studio:** Brian Landes, a professional Unity developer.
- **One product: [Narramancer](https://narramancer.com).** It's a Unity 6 plugin, node graphs with save and
  load built in. Version 1.0.0 is in Asset Store review as of 2026-10-30, relaunching at $39.
- **The domain currently hosts a game pitch**, *Pseudo World*, a third-person action game that's on the back
  burner. The new site **replaces** it. What happens to the pitch is open question Q2 below.

**What the site is for:** credibility. A buyer looking at a $39 save system asks *"who makes this, and will they
still be around in a year?"* That's the main objection for a tool you trust with players' save files. The
Asset Store publisher page, the Narramancer listing and narramancer.com all link here, and this site has to
answer that question in a few seconds. It isn't a second sales page for Narramancer, and it isn't a portfolio.

---

## 2. Open questions — settle these with Brian first

| # | Question | Recommendation |
|---|---|---|
| Q1 | Keep the name "Professional Bad Guys"? | **Yes.** An old task suggested renaming it as irrelevant to save-system buyers. Building this site settles it the other way. The name is memorable, and the brand's job is to make it read as *professional* first. |
| Q2 | What happens to the Pseudo World pitch? | Take it off the home page. Either move it to `/games/pseudo-world` marked "on hold", or archive it off-site. Its $75k funding ask and April 2026 alpha date are out of date, and a stale roadmap undercuts the "still around in a year" message. |
| Q3 | Studio voice ("we") or personal ("I, Brian")? | **Studio name, personal About page.** One person behind a clearly named studio reads as dependable, and naming the human builds trust. Don't pretend to be a team. |
| Q4 | Domain | Confirm the domain the Pseudo World pitch is on, and whether it stays. The Asset Store publisher page should point at it. |
| Q5 | Support contact | One address for Narramancer support, e.g. `support@<domain>`. The Asset Store expects a support contact, and the site should show the same one. |
| Q6 | Where do brianlandes.com, LinkedIn, Bluesky and Reddit fit? | Link them from About only, not the site header. |

---

## 3. Brand guide outline

Fill each heading in with Brian. The notes under each are starting points, not decisions.

### 3.1 How the brands fit together
- **Professional Bad Guys** is the parent, the publisher. **Narramancer** is a product brand with its own
  identity, already defined in its
  [brand guide](https://github.com/BrianLandes/Narramancer/blob/main/docs/BRAND_STYLE_GUIDE.md).
- The parent **endorses** its products; it doesn't absorb them. On the PBG site Narramancer appears in its own
  colours, the way a publisher shows its titles. On narramancer.com, PBG is a footer line: "by Professional Bad
  Guys".
- **So PBG's visual identity has to be quieter than its products.** It's a frame that any future product,
  including a game, can sit inside without clashing.

### 3.2 The core idea
- The tension to use: **"Professional"** (dependable, craftsmanlike, on time) against **"Bad Guys"** (playful,
  a wink). Professional is the substance; Bad Guys is the personality. Getting that order backwards turns it
  into a joke brand.
- Possible directions. Pick one:
  - **"Every story needs a villain."** The bad guys are the ones who build the machinery behind the scenes,
    which ties neatly to narrative tools.
  - **Corporate villainy, played straight.** The mock-serious tone of an evil organisation that is very good
    at its job: org charts, ID badges, departments. It's fun, but it needs restraint.
  - **The quiet professional.** Mostly understated, with the wink kept to one place: the name, the logo, the
    404 page.
- One line to test the direction against: *"We make tools you can trust with the hard parts."*

### 3.3 Voice and tone
- Plain, confident, a little dry. Short sentences.
- The wit lives in small places (the name, microcopy, the 404 page), never in product claims or support text.
- Never "open source" for Narramancer: it's **source-available**. Don't overclaim. Use the product's own
  wording.
- The Pseudo World pitch's voice ("bombastic", "Secret Sauce") belongs to that game, not the studio.

### 3.4 Logo
- A wordmark, plus a small mark for the favicon, social avatars and the Asset Store publisher logo.
- Directions to explore: a **villain mustache or monocle** silhouette, done minimal; a **domino mask** shape;
  **"PBG"** monogram in an ID-badge or business-card frame. Avoid skulls, horns and devil tails: too loud,
  and too close to game genres the studio isn't selling.
- Deliverables: wordmark (horizontal and stacked), mark alone, one-colour versions, a 160×160 avatar, and a
  favicon set.

### 3.5 Colour
- **Mostly neutral.** A near-black or charcoal base with an off-white paper colour, so product colours carry
  the page.
- **One studio accent**, used sparingly. It must not be Narramancer's pink `#EF476F` or anything near it,
  because the products bring their own accents. Candidates: a villain **gold/amber**, an **acid green**, or a
  deep **oxblood**. Check each for WCAG AA contrast on both base colours.
- Define both light and dark themes from the start, as tokens.

### 3.6 Type
- One sans for UI and body, plus one display face or heavy weight for the wordmark and headings. A mono is
  optional, for technical pages.
- Keep to free, web-licensed fonts (Google Fonts) so the guide is portable.
- Narramancer uses Open Sans in its shipped UI. PBG can differ; the two are never set side by side.

### 3.7 Imagery and motif
- **Real product media first:** Narramancer screenshots and the demo video. Stock photos and AI-looking art
  read as untrustworthy for a tool vendor.
- One recurring studio motif, from whichever direction 3.2 picks: an ID badge, a dossier, a stamp. Use it in
  small doses.
- No generic dev-tool clichés: gears, `</>`, circuit boards.

### 3.8 Applications
The guide should show the brand on: the site header and footer, the Asset Store publisher page (logo and
banner), social avatars, email signature, and the one-line "by Professional Bad Guys" endorsement used on
product sites.

---

## 4. Site outline

Small and fast. Five pages at most to start.

| Page | Purpose | Contents |
|---|---|---|
| **Home** | Answer "who is this?" in five seconds | Logo, a one-line studio statement, **Narramancer featured** as the product card (see §5), a short "about the studio" teaser, footer |
| **Products** (or folded into Home while there's only one) | Room to grow | Narramancer card; later, other tools or games |
| **About** | Trust | Brian: who, background in professional Unity work, why the studio exists, links to Q6 profiles |
| **Support / Contact** | Required by the Asset Store and expected by buyers | Support email, Narramancer docs link, a GitHub issues link, response-time expectation |
| **Games / Pseudo World** | Only if Q2 says to keep it | Marked "on hold", no funding ask, no dated roadmap |

- **Footer:** © Professional Bad Guys, support email, the Asset Store publisher page, a privacy note if any
  analytics are added.
- **Technical:** static is fine, and preferred. Set `og:image` per page, make it phone-friendly, and keep it
  fast. Keep the build simple enough that one person can update it in ten minutes.

---

## 5. Pointing at Narramancer

| Use | Value |
|---|---|
| Product site | https://narramancer.com (being reworked in parallel; see [its handoff](https://github.com/BrianLandes/Narramancer/blob/main/docs/NARRAMANCER_COM_HANDOFF.md)) |
| Asset Store | https://assetstore.unity.com/packages/tools/visual-scripting/narramancer-269301 |
| Source (source-available) | https://github.com/BrianLandes/Narramancer |
| Demo video | YouTube, currently Unlisted. **Brian has the link** and makes it public on launch day. |
| One-liner | *Node graphs for Unity 6 with save and load built in. Save mid-scene, quit, reload, and every graph picks up on the exact node it was on.* |
| Product card art | [`docs/media/store/card-420x280.png`](https://github.com/BrianLandes/Narramancer/blob/main/docs/media/store/card-420x280.png), or the cover/social images alongside it |
| Icon | [`docs/media/store/icon-160x160.png`](https://github.com/BrianLandes/Narramancer/blob/main/docs/media/store/icon-160x160.png) |
| Wording rules | [`docs/LAUNCH_COPY.md`](https://github.com/BrianLandes/Narramancer/blob/main/docs/LAUNCH_COPY.md): say "source-available", never "open source"; don't claim untested platforms |

Copy the media into the site repo; don't hot-link GitHub. **Don't mention the price** on this site: it lives on the
Asset Store and can change there without the site going stale.

**Timing:** the Narramancer relaunch is waiting on Asset Store approval. The PBG site can launch before that, but
it should link to narramancer.com and the listing, not describe 1.0 features that aren't on sale yet.

---

## Done when

- [ ] Q1–Q6 answered and written at the top of the brand guide
- [ ] The brand guide is filled in from §3: tokens for colour (light and dark) and type, logo files, voice rules
- [ ] Logo set exported, including the Asset Store publisher logo and avatar
- [ ] Site live with Home, About and Support, and Narramancer featured
- [ ] Pseudo World handled per Q2; no funding ask or dated roadmap on the studio home page
- [ ] The Asset Store publisher page updated: logo, website link, support email
- [ ] narramancer.com footer says "by Professional Bad Guys", linking here
