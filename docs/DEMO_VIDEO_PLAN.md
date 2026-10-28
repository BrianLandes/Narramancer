# Demo video plan

The §3 launch video: **60–90 seconds, no narration, captions only.** One idea, shown so it can't be faked:
*a story is saved mid-scene, the editor is quit completely, and loading picks it up exactly where it was.*

**Who does what:**
- **You record** the raw clips. It's your screen, and the key shot needs Unity actually quit and reopened.
- **I edit** with the tools already installed (`ffmpeg` with text overlays and crossfades). That covers trims,
  speed-ups, captions in the brand style, title and end cards, transitions, and every export format below.

---

## Before recording

- [ ] **Clear old saves.** Delete everything in `Assets/Saves/`, so the slot list shows only what you make on
      camera.
- [ ] **Window layout at 1920×1080.** Maximize Unity on a 1080p display, or record a 1920×1080 region.
  - Put the **Game view** on the left, about 60% of the width, set to **16:9**.
  - Put the **graph window** for *Text and Choices Example Verb* on the right, zoomed so node titles are
    readable.
  - Hide the Console, Scene view and Project window; Inspector is optional.
- [ ] **Dark editor theme.** It matches the samples.
- [ ] **Turn off notifications.** The recorder captures them too.
- [ ] **Recorder settings** in SimpleScreenRecorder (already installed): 60 fps, H.264, CRF 18 or lower, no
      audio, cursor visible.
- [ ] **One file per shot**, named `shot1.mkv`, `shot2.mkv` and so on. Start each about 2 s early and stop about
      2 s late, so I have room to cut.
- [ ] **Move the mouse slowly and deliberately.** Pause about a second before each click.
- [ ] **Do one dry run** of the whole sequence before recording.

## Shot list

Timings are for the finished cut. Record each shot at normal speed; I handle the pacing.

| # | Cut length | What you do | Caption (added in the edit) |
|---|---|---|---|
| 1 | — | Nothing; I make it. Title card: icon, "Narramancer", "Save anywhere. Even mid-scene." | — |
| 2 | ~10 s | Open sample 1, press **Play**. Let the first text reveal, click once, and let the second reveal. The running node glows pink in the graph on the right. | *A Narramancer verb, running* |
| 3 | ~8 s | Stop on a passage that is still waiting for a click. Click **Save**, then **+ New Save**, and the new slot appears with its thumbnail. Click **Close**. | *Save, mid-scene* |
| 4 | ~6 s | Click through one or two more passages, so the story has clearly moved on past the save. | *Keep playing…* |
| 5 | ~8 s, sped up | **File → Exit.** Unity closes. Show Unity Hub or the desktop, then reopen the project and wait for it to load. Don't cut away: I'll speed this part up 8–12× with a visible "▶▶" badge, so it's obviously real and not edited out. | *Quit the editor. All the way.* |
| 6 | ~10 s | Press **Play**; the story starts from the beginning. Click **Load** and pick the slot. The scene reloads at the saved passage, and the glow in the graph is back on the same node. Pause there for 2 s. | *Load. It picks up exactly where it left off.* |
| 7 | ~12 s | Open sample 4 and press Play. Assign a weapon (the sprite, name and stats change), then Save. Assign something else, then Load: the first weapon is back. | *Scene objects too* |
| 8 | ~6 s | Optional. In sample 4, select an object with a **Saveable** and show its inspector toggles. Hover over them, but don't change anything. | *One component decides what's saved* |
| 9 | — | Nothing; I make it. End card: "Node graphs with built-in save/load", "Unity 6", "Asset Store", narramancer.com. | — |

That comes to about 60–70 seconds with shot 8 and about 55 without it.

### If a take goes wrong

- **Clicked too fast, or the cursor wandered:** re-record just that shot. Shots are cut independently.
- **The glow didn't move after loading (shot 6):** stop and tell me. That would be a bug. It was fixed and
  checked on 2026-10-28 (commit `6c19d58`), but the video depends on it.
- **The save slot thumbnail is blank:** it's taken from the frame before the menu opens. Make sure the Game
  view is visible and not covered when you click Save.

## What I'll do with the clips

Drop the files in a folder and tell me where, and I'll:

1. **Trim** each shot to its action and speed up the restart in shot 5, with the "▶▶" badge.
2. **Add captions** in Open Sans on a navy bar with an accent rule, matching the samples. They fade in and out,
   and each stays up long enough to read without sound.
3. **Make the title and end cards** from the icon and palette, then join everything with short crossfades.
4. **Export:**
   - a **1080p60 MP4** (H.264) for YouTube or Vimeo, which is what the Asset Store listing links to;
   - a **muted loop for the README**, as WebM plus a GIF under 10 MB, covering shots 3–6 only (the save, quit and
     resume);
   - **stills** from the best frames, to use as listing screenshots.

You review the cut, and I revise. Uploading to YouTube and the Asset Store is yours to do.

## Optional: a stronger "mid-action" shot

Text waiting for a click is a real suspended verb, but it reads as "paused" more than "in motion". A shot
where something is **visibly moving** when the save happens is more convincing. For example, a character
sliding across the screen is saved halfway and resumes the slide after the reload. No sample does that yet, so
nothing in a build tests a tween resuming after a load either (see `tasks.md`). If you want it, I can add a short
"walk on" to sample 4 before you record. That fills the test gap and adds a shot to the video.
