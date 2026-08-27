# Experiment: are saved asset references stable across builds?

**Question.** Odin writes external Unity references into the save as `"objects": [{"instanceID": 4206}, ...]`.
An `instanceID` is assigned by the *running process*, not stored with the asset. Does a save written by one
build still resolve correctly in a later build of the same game?

**Why it matters.** If the answer is no, then **shipping a patch invalidates your players' saves** — the worst
available failure for a plugin sold on its save system. It also promotes the GUID-keyed reference resolver in
[`ODIN_REMOVAL_PLAN.md`](ODIN_REMOVAL_PLAN.md) from a nice-to-have to a required fix, and turns the Odin
removal from a refactor into a correctness fix.

**Design note.** The experiment does *not* rely on the breakage being visible in-game. It measures the
instanceIDs directly, so it gives a clear answer either way. `tools/inspect-save.py` does the comparison.

---

## Test 1 — the 10-minute version (editor)

Proves or disproves the *mechanism* cheaply. If it doesn't reproduce here, don't spend an hour on Test 2.

1. Open `Assets/Narramancer/Scenes/1. Text and Choices Example`.
2. Press Play. Advance a few lines of dialogue, then save from the in-game menu.
   Editor saves land in **`Assets/Saves/`**.
3. Stop play mode and copy the save aside:
   ```bash
   cp "Assets/Saves/SaveSlot_001.json" /tmp/save-session-A.json
   ```
4. **Fully quit Unity and reopen the project.** (A domain reload is not enough — instanceIDs survive that.)
5. Press Play, reach roughly the same point, and save again — a second slot.
6. Compare:
   ```bash
   python3 tools/inspect-save.py /tmp/save-session-A.json "Assets/Saves/SaveSlot_002.json"
   ```
7. Then the user-facing half: still in this session, **load `SaveSlot_001`** (the one from session A) and see
   what happens — right text, wrong text, nothing, or an error.

---

## Test 2 — the real one (builds)

Test 1 shows instanceIDs move between editor sessions. Test 2 answers the question that actually matters:
does a **shipped patch** break existing saves?

### Set up a clean slate

```bash
mv "$HOME/.config/unity3d/Professional Bad Guys/Narramancer/Saves" \
   "$HOME/.config/unity3d/Professional Bad Guys/Narramancer/Saves.backup"
```

### Build A — the "released game"

1. Build the project (any desktop target; the default Mono backend is fine — this is not an AOT test).
2. Run the build. Advance a few lines of dialogue. Save.
3. Confirm it works: load that save **in the same build**. It should be fine — this is the control, and it's
   the scenario that has always passed.
4. Preserve it:
   ```bash
   cp "$HOME/.config/unity3d/Professional Bad Guys/Narramancer/Saves/SaveSlot_001.json" /tmp/save-build-A.json
   ```

### Change the content — the "patch"

The goal is to shift the load order of the assets the save points at. Those are sub-assets of
**`Text and Choices Example Verb.asset`**, so perturbing that graph is the most direct lever:

- Open the verb graph and **add ~5 nodes** anywhere (type doesn't matter, they need not be connected). Save.

Fully reversible afterwards with:
```bash
git checkout -- "Assets/Narramancer/Scenes/1. Text and Choices Example/Text and Choices Example Verb.asset"
```

### Build B — the "patched game"

5. Rebuild to the same output folder.
6. Run build B and **save fresh** (a new slot) from roughly the same story point.
7. Compare the two builds' references:
   ```bash
   python3 tools/inspect-save.py /tmp/save-build-A.json \
     "$HOME/.config/unity3d/Professional Bad Guys/Narramancer/Saves/SaveSlot_002.json"
   ```
8. Then load **build A's save inside build B** and observe.

### Restore

```bash
git checkout -- "Assets/Narramancer/Scenes/1. Text and Choices Example/Text and Choices Example Verb.asset"
rm -rf "$HOME/.config/unity3d/Professional Bad Guys/Narramancer/Saves"
mv "$HOME/.config/unity3d/Professional Bad Guys/Narramancer/Saves.backup" \
   "$HOME/.config/unity3d/Professional Bad Guys/Narramancer/Saves"
```

---

## Reading the result

| Outcome | Means | Do |
|---|---|---|
| **IDs differ** | Confirmed. Saved asset references don't survive a content change. Patching breaks saves. | GUID-keyed references become **required**. The Odin removal is a correctness fix, not a refactor. |
| **IDs match** | The disruption wasn't enough to move them, *or* they're stabler than expected. | Try a bigger disruption first. If they still match: this is a **latent** risk — nothing in Unity promises instanceID stability — so still fix it, but it drops down the priority list. |
| **Load visibly breaks** | The user-facing consequence, whatever the IDs say. | Capture the console output; it tells us how it fails (wrong asset vs null). |

**Either way it's a good hour.** A confirmed break makes the case for the GUID work unarguable; no break means
the port is lower-risk than feared and can be sequenced more calmly.

Record the outcome in `done.md` — it's a decision input for the whole §2 ordering.
