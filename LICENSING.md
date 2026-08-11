# Licensing

Plain-language summary. The authoritative terms are in [`LICENSE`](LICENSE) — if anything here disagrees with
that file, that file wins. This is not legal advice.

## The short version

| You want to… | Allowed? |
|---|---|
| Read the source, study it, learn from it | ✅ Yes, freely |
| Clone, fork, modify, experiment | ✅ Yes, freely |
| Use it in a hobby project, game jam, or student work | ✅ Yes, freely |
| Use it in a game or tool you intend to sell or monetize | ⚠️ **Requires an Asset Store purchase** |
| Use it at work / inside a company product | ⚠️ **Requires an Asset Store purchase** |

**License:** [PolyForm Noncommercial 1.0.0](https://polyformproject.org/licenses/noncommercial/1.0.0).
**Commercial license:** buy Narramancer on the
[Unity Asset Store](https://assetstore.unity.com/packages/tools/visual-scripting/narramancer-269301).

This is a **dual-licensing** model: the same code, offered under a free noncommercial license here and a
commercial license through the Asset Store.

## Say "source-available," not "open source"

PolyForm Noncommercial is **not** an OSI-approved open-source license, because it restricts a field of use.
Calling it "open source" is inaccurate and, in developer communities, actively counterproductive — the
distinction gets policed hard, and getting it wrong turns a strength into an argument.

- ✅ "Full source available on GitHub"
- ✅ "Source-available"
- ❌ "Open source"

This applies to the store listing, the README, narramancer.com, and any forum or Reddit post.

## Prior versions stay Apache-2.0

Narramancer was previously published under the Apache License 2.0. **Everything published under Apache-2.0
stays under Apache-2.0, permanently.** That grant is irrevocable — it cannot be withdrawn, and this change does
not attempt to. Anyone who obtained the code before the relicense keeps those rights for those versions.

The change applies **going forward only**: to the commit that introduced the new `LICENSE` file and everything
after it. The previous Apache-2.0 text is preserved at [`LICENSE-APACHE-2.0.txt`](LICENSE-APACHE-2.0.txt).

The practical exposure is near zero — the relicense happened while the repo had negligible traffic, which is
exactly why it was worth doing now rather than later.

## What the license does *not* cover

The license covers **Narramancer's own source code**. It does not cover the third-party components bundled in
this repository, which keep their original licenses and may be used under those terms:

| Component | License | Location |
|---|---|---|
| OdinSerializer (TeamSirenix) | Apache-2.0 | `Assets/Narramancer/Plugins/OdinSerializer/` |
| xNode (Thor Brigsted / Siccity) | MIT | `Assets/Narramancer/Scripts/Plugins/xNode/` |
| Unity-SerializableDictionary (azixMcAze) | MIT | `Assets/Narramancer/Scripts/Plugins/SerializableDictionary/` |
| SerializableAction / UndoPro (Seneral) | MIT | `Assets/Narramancer/Scripts/Plugins/SerializableAction/` |
| Open Sans | SIL OFL | `Assets/Narramancer/Fonts/Open_Sans/` |

Full attribution is in [`Assets/Narramancer/Third-Party Notices.txt`](Assets/Narramancer/Third-Party%20Notices.txt).
All of these are permissive, so bundling and redistributing them inside a commercial product is fine — but they
are **not ours to relicense**, and their terms are unaffected by anything here.

> Note: OdinSerializer is scheduled for removal (see [`tasks.md`](tasks.md) §2). When it goes, drop its entry
> from the table above and from the Third-Party Notices.

## "What if you abandon the project?"

This is the load-bearing objection for a save system, and it's worth being straight about how the license
change affects the answer.

- **You can always read the source.** The full implementation is public and stays public. Nothing is a black
  box, and you can understand exactly what your save files contain.
- **An Asset Store purchase is a perpetual license** to the version you bought, including the right to modify
  it for use in your own projects. You are not renting access.
- **What changed:** under Apache-2.0, a non-purchaser could have forked and shipped commercially. They now
  can't without buying. For anyone who *has* bought, nothing changed — you keep source access and modification
  rights either way.

## Contributions

The project has a single copyright holder, which is what made this relicense possible. If that changes, note
that accepting outside contributions under a source-available license generally needs a contributor agreement
(a CLA or a DCO with an explicit license grant) — otherwise contributions land under terms that may block a
future licensing change. Worth setting up *before* accepting the first external pull request, not after.
