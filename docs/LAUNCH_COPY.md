# Launch copy — YouTube and the Asset Store submission

Paste-ready text for the 1.0.0 relaunch (§3 in [`tasks.md`](../tasks.md)). The reasoning behind the positioning is
in [`V1_ROADMAP.md`](V1_ROADMAP.md) §3, A2. Media is in [`media/`](media/). The editable masters are in
`~/Videos/narramancer-demo/`, and `_edit/store_images.py` there rebuilds the marketing images.

**Rules that apply to every field below:**
- Say **"source-available"** or **"full source included"**, never "open source". See [`LICENSING.md`](../LICENSING.md).
- **Lead with the problem**, not the mechanism. The selling point is that saving a *running* graph is built in,
  not that the tool uses node graphs.
- **Don't claim more than has been tested.** Save/load has been verified in WebGL (IL2CPP) player builds and in
  the editor on Linux, using the Built-in render pipeline. Nothing has been tested on mobile or consoles.

---

## YouTube

**Title** (68 characters):

```
Save a Unity Story Mid-Scene, Quit, and Resume Exactly | Narramancer
```

Shorter alternative: `Narramancer: Node Graphs with Built-In Save/Load (Unity 6)`

**Description:**

```
Narramancer is a node-graph tool for Unity 6 where saving and loading are built into how graphs run.

In this video: a story is saved halfway through a scene, the Unity editor is quit completely, and loading picks the story back up on the exact node it stopped on. Then a scene's objects (sprites, names, stats) are saved and restored the same way.

No narration. Every step is real; the editor restart is only sped up.

▸ Asset Store: https://assetstore.unity.com/packages/tools/visual-scripting/narramancer-269301
▸ Documentation: https://narramancer.com
▸ Source on GitHub (source-available): https://github.com/BrianLandes/Narramancer

What gets saved:
• Every running graph, including what it is waiting on: a click, a choice, a timer, a character mid-walk
• Characters, items and places, with their properties, stats and relationships
• Every variable
• Scene objects you mark with one Saveable component
• Fields of your own scripts, by adding [Save]

Chapters
0:00 A verb, running
0:12 Save, mid-scene
0:26 Quit the editor, reopen, load
0:45 Scene objects too
1:06 One component decides what's saved

#unity #gamedev #unity3d #indiedev #visualnovel
```

**Other settings:**
- **Thumbnail:** `media/still-3-loaded-after-restart.png` is the clearest single frame. A custom thumbnail
  needs a verified YouTube account. If yours isn't verified, the auto-picked frame will do.
- **Visibility:** **Unlisted** until the Asset Store approves the submission, then **Public** on launch day. The
  link works for the submission either way.
- **Category:** Science & Technology. **Audience:** not made for kids.
- **Tags:** `unity save system, unity save and load, unity node graph, unity visual scripting, unity dialogue
  system, visual novel unity, unity 6, game state, save anywhere`

---

## Asset Store submission

### Package name

```
Narramancer: Node Graphs with Built-In Save/Load
```

**Why not "Narramancer - Save and Load System":** that name puts it up against Easy Save and the other dedicated
save libraries. Those are serializers that sit beside your scripts, so a buyer searching "save system" who wants
a serializer will bounce. Narramancer is a graph tool whose graphs happen to be saveable, which is close to a
category of one. The name should say both what it *is* (node graphs) and what's special (save/load). The
keywords below still catch the "save system" searches.

If the form rejects the length or the colon, use `Narramancer - Save/Load Node Graphs`.

### Category

**Keep Tools > Visual Scripting.** The launch's success check is the asset showing up when you browse Visual
Scripting filtered to 6000.x, and buyers comparing node tools browse there. The listing id (269301) stays the same
whatever the category, so moving it isn't worth losing that check. narramancer.com still links to an old
`/behavior-ai/` URL; the site handoff covers fixing that.

### Price

**$39**, with no launch discount. The roadmap explains why: $7.99 reads as abandonware for a tool you would trust
with your saves.

### Version and compatibility

| Field | Value |
|---|---|
| Version | `1.0.0` |
| Unity version | Unity 6 (`6000.x`). Upload from **6000.4.12f1**, the version it's built and tested on. |
| Render pipelines | **Built-in: compatible (tested).** URP and HDRP: the runtime has no shaders or materials and the UI is uGUI, so it should be pipeline-independent. But nobody has opened the samples in a URP or HDRP project. Either spend 15 minutes importing into a URP project and pressing Play, or mark only Built-in. Don't tick what hasn't been seen. |
| Dependencies | None beyond Unity UI (`com.unity.ugui`, on by default). The NavMesh nodes use the built-in AI module and drop out cleanly without it. |

⚠️ The Asset Store records the Unity version the package was **uploaded from** as the minimum. Uploading from
6000.4 means anyone on Unity 6.0 LTS sees a "newer version" warning. That's acceptable for 1.0.0. If 6.0 LTS
users turn out to matter, a later upload from 6000.0 would widen it.

### Keywords

Swap out the old generic set (`logic`, `Tool`, `Node`, `flow`, `visual`, `Graph`):

```
save system, save and load, save anywhere, game state, serialization, checkpoint, visual novel, dialogue, branching dialogue, node graph, behavior tree, quest
```

If there's a cap, keep the first eight. They're the high-intent terms from the roadmap.

### Summary

The short line under the title (about 160 characters):

```
Node graphs for story, dialogue and game logic that can be saved at any moment, even mid-action, and resumed exactly where they stopped. Unity 6.
```

### Description

The form's editor takes basic formatting. Make the headings bold and turn the `•` lines into real bullet lists
after pasting.

```
Save anywhere. Even mid-scene.

Your player saves halfway through a conversation. A character is mid-walk, a timer is counting down, and the game is waiting on a choice. Most scripting tools can't save that moment: Unity Visual Scripting and Fungus don't record which step is running, so saving mid-sequence is something you build yourself, or forbid.

In Narramancer, saving is part of how graphs run. Save at any moment, quit, relaunch and load, and every graph picks up on the exact node it was on, still waiting for the same click. The video above shows it with the editor quit completely in between.

WHAT GETS SAVED
• Every running graph, including what it is waiting on: a click, a choice, a timer, a character halfway through a walk
• Every noun (your characters, items and places) with its properties, stats and relationships
• Every variable, global or scene
• Scene objects you mark with one Saveable component: transforms, sprites, images, text, audio, animation, NavMesh agents and active state
• Fields of your own scripts: mark them [Save] and they come back after a load, with no other code

BUILD WITH GRAPHS
• Over 150 node types for text, choices, variables, lists, timers, movement, animation and scene objects
• ActionVerbs run over time and can wait; ValueVerbs answer a question instantly
• A built-in table of the things in your game, with properties, stats and relationships
• Edit graphs in Play Mode, and watch the running node light up as the game plays

BUILT TO BE TRUSTED WITH SAVES
• Saves point at objects by permanent ids, so renaming, reparenting or reordering objects doesn't break existing saves
• Saves are readable JSON
• Save, quit, relaunch and load has been verified in IL2CPP (WebGL) player builds
• Full C# source included
• No dependencies, no Odin, no Player Settings changes

INCLUDED
• Five sample scenes: a launcher, branching text and choices, creating nouns at runtime, properties, and a character equipment screen
• Drop-in UI prefabs: text and choice printers, a save/load menu with thumbnail slots, and a history log
• A Getting Started guide (PDF), plus full documentation at narramancer.com

GOOD FOR
Visual novels and branching dialogue, quest and story flow, NPC behaviour, and the overall loop of your game.

Upgrading from 0.5? Version 1.0 changes the save format. See the release notes and chapter 7 of the Getting Started guide.

Narramancer uses xNode and Unity-SerializableDictionary under the MIT license, and Open Sans under the SIL Open Font License; see the Third-Party Notices.txt file in the package for details.
```

The last paragraph is the third-party disclosure the Asset Store asks for. Keep it in the description even if
the form also has a separate field for it.

**On mentioning GitHub here:** the roadmap wants "full source on GitHub" as a selling point. But a store
listing that links to a free copy of the same code may get questioned in review, even though the license makes
that copy noncommercial-only. This draft says "Full C# source included", which is true of the package and
risk-free, and leaves GitHub to the README, YouTube and narramancer.com. If you want the link in anyway, add
`Developed in the open: github.com/BrianLandes/Narramancer (source-available; commercial use requires this
license).` after the source bullet.

### Technical details

```
• Unity 6 (6000.x). Built and tested on 6000.4.12f1.
• Render pipeline independent: the runtime has no shaders or materials, and the UI uses Unity UI (uGUI).
• Pure C#, no native plugins. Save/load verified in IL2CPP (WebGL) player builds.
• No package dependencies beyond Unity UI. NavMesh nodes use the built-in AI module and are left out cleanly without it.
• Saves are JSON: Assets/Saves in the editor, Application.persistentDataPath/Saves in a build, and IndexedDB on WebGL (survives a page reload).
• Built-in serializer. Assets are saved by permanent id, through an asset manifest rebuilt before every build. Scene objects are saved by a Saveable's id, never by name or hierarchy path.
• Over 150 node types. Graph editor built on xNode (included).
• Full C# source. Runtime and editor code are in separate assemblies, under the Narramancer namespace.
• Documentation: Getting Started PDF in the package; full docs at narramancer.com.
• Not compatible with 0.5 saves. Upgrade steps are in Getting Started, chapter 7.
```

### Release notes — 1.0.0

```
1.0.0 is a major release: Unity 6, a new save system underneath, and a reworked way to save scene objects. It changes the save format, so 0.5 saves can't be loaded (they're rejected with a message, not misread).

New
• Unity 6 support (6000.x).
• Saveable: one component per object decides what gets saved (transform, sprites, images, text, audio, animation, NavMesh agent, active state). It replaces eight of the Serialize… components; Serialize Noun Instance Reference is unchanged.
• Scene objects have permanent ids. Renaming, reparenting or reordering an object no longer breaks saves.
• [Save] attribute: mark a field on any MonoBehaviour and it's saved with its object.
• Movement, tween, animation and NavMesh nodes resume after a load: a character saved mid-walk finishes the walk.
• Window > Narramancer > Add Missing Saveables, for upgrading scenes and prefabs in bulk.
• Sample Launcher scene; all samples and UI prefabs restyled.
• A toolbar for the graph editor: back, recent verbs, find verb, add node, frame and more.
• Rewritten Getting Started guide.

Changed
• Odin Serializer removed and replaced by a built-in serializer. The .NET 4.x Player Setting is no longer needed, and the package no longer ships a copy of Odin.
• Assets in saves are referenced by permanent id instead of per-session ids, so saves survive rebuilding the project.
• Inequality node: the old "Greater Than" and "Less Than" options were actually ≥ and ≤. They're relabelled as such, so existing graphs behave exactly as before, and strict > and < options are added.

Fixed
• Stopping a verb now stops every verb it started.
• Cancelling a node stops its timer, and saved timers resume with the right time remaining.
• Rect and Bounds values are saved (they were silently dropped).
• Choices are saved per printer instead of under one shared key.
• Renaming a verb input keeps the values assigned to it.
• The continue indicator shows only when a click is actually awaited.
• Many smaller fixes; over 570 automated tests now cover saving, loading and resuming.

Upgrading from 0.5: delete any leftover OdinSerializer folder, replace Serialize… components with Saveable, then run Add Missing Saveables. Full steps: Getting Started, chapter 7.
```

### Media tab

| Slot | File | Notes |
|---|---|---|
| Icon 160×160 | `media/store/icon-160x160.png` | Same as the package's own icon |
| Card 420×280 | `media/store/card-420x280.png` | Title text only, as required |
| Cover 1950×1300 | `media/store/cover-1950x1300.png` | Title text only, as required |
| Social 1200×630 | `media/store/social-1200x630.png` | No text, as required |
| Video | the YouTube link | Put it **first** in the carousel |
| Screenshots | `media/still-1` … `still-5` | In this order: 3 (loaded after restart), 2 (save mid-scene), 1, 4, 5 |

Delete the six old screenshots. They show 0.5's UI and the old `Serialize*` components.

### Things to check in the form

- **Generative AI disclosure:** if the form asks whether AI tools were used, answer accurately. Code, docs and
  these marketing images were made with Claude's help.
- **Package contents:** upload `Assets/Narramancer` only, not `Assets/Saves`, `Assets/Test Suite` or `Assets/Resources`.
  Then check the file list for stray save files.
- Submitting 1.0.0 still depends on the **patch test** passing. The **Odin Inspector coexistence check** must also
  be done first, or at least before you claim it publicly. Both are in [`tasks.md`](../tasks.md).
