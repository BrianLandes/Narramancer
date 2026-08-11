# Handoff: WebGL saves don't survive a page refresh

**Symptom:** In a WebGL build, saving works *in-session* (save → load in the same tab is fine), but after a
page refresh the save is gone.

**This is a known Unity WebGL issue, not a Narramancer bug — and v2 already fixed the identical problem.**
This doc has the root cause + a proven, copy-paste-ready fix adapted to v1.

## Root cause
On WebGL, `Application.persistentDataPath` is an **in-memory filesystem (IDBFS)**. `File.WriteAllText` writes
to that in-memory FS, but the bytes only reach the browser's durable **IndexedDB** when **`FS.syncfs(false)`**
is called. Unity does **not** call it automatically after a file write — so the save lives only in memory and
is lost on the next page load. (The load direction is fine: Unity auto-syncs IndexedDB → memory during
startup, so once the data is actually *in* IndexedDB, reads work.)

v1's `SaveLoadUtilities.WriteSaveData` (`Assets/Narramancer/Scripts/Utilities/SaveLoadUtilities.cs:35`) does a
plain `File.WriteAllText(filePath, jsonData)` with **no flush** → this is the whole bug.

## The fix (three small pieces)
1. A **`.jslib`** WebGL plugin that calls `FS.syncfs(false, …)`.
2. A **guarded C# wrapper** — the real `syncfs` on WebGL builds, a **no-op** everywhere else (desktop writes
   are already durable).
3. **Call the flush right after the write**, inside `WriteSaveData`, so every caller is covered.

### 1. `Assets/Narramancer/Scripts/Plugins/WebGL/SaveFileSync.jslib`
> Must live under a `Plugins/WebGL/` folder (or have the WebGL platform ticked on its `.meta`) for Unity to
> compile it as a native WebGL plugin. The exported function name **must exactly match** the `DllImport` name
> in step 2 (`NarramancerFlushSaves`).
```javascript
// WebGL save persistence: flush the in-memory filesystem (IDBFS) to the browser's IndexedDB.
// On WebGL, Application.persistentDataPath only reaches IndexedDB when FS.syncfs is called; Unity doesn't do
// this after a File write, so without it a save is lost on the next page load.
mergeInto(LibraryManager.library, {
  NarramancerFlushSaves: function () {
    try {
      if (typeof FS !== 'undefined' && FS.syncfs) {
        // populate=false => persist FROM memory TO IndexedDB.
        FS.syncfs(false, function (err) {
          if (err) console.error('[Narramancer] FS.syncfs (save flush) failed: ' + err);
        });
      }
    } catch (e) {
      console.error('[Narramancer] NarramancerFlushSaves threw: ' + e);
    }
  },
});
```

### 2. `Assets/Narramancer/Scripts/Utilities/SaveFileSync.cs`
> Runtime assembly (`Narramancer.asmdef`) — same assembly as `SaveLoadUtilities`, so no asmdef changes.
```csharp
namespace Narramancer {
    /// <summary>
    /// Persists save files on WebGL. There, <c>Application.persistentDataPath</c> is an in-memory filesystem
    /// (IDBFS) whose contents only reach the browser's IndexedDB when <c>FS.syncfs</c> is called — Unity
    /// doesn't do that after a <c>File.Write</c>, so a save is silently lost on the next page load.
    /// <see cref="Flush"/> forces it via the SaveFileSync.jslib plugin; off WebGL it's a no-op (writes there
    /// are already durable). Call it right after writing or deleting a save file.
    /// </summary>
    public static class SaveFileSync {
#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void NarramancerFlushSaves();

        public static void Flush() => NarramancerFlushSaves();
#else
        public static void Flush() { }
#endif
    }
}
```

### 3. Call it after the write — `SaveLoadUtilities.WriteSaveData`
```csharp
public static void WriteSaveData(string saveName, string jsonData) {
    var saveDirectory = GetSaveDataDirectory();
    Directory.CreateDirectory(saveDirectory);
    var filePath = $"{saveDirectory}/{saveName}.json";
    File.WriteAllText(filePath, jsonData);
    SaveFileSync.Flush();   // <-- ADD: persist to IndexedDB on WebGL (no-op elsewhere)
}
```
> Putting the flush **inside `WriteSaveData`** covers every caller automatically (today just `SaveMenu.cs:89`).
> **If a delete-save path is added later, call `SaveFileSync.Flush()` after the `File.Delete` too** — a
> deletion also needs to be persisted to IndexedDB, or the "deleted" save reappears on reload.

## Why this is the whole fix (v1 specifics I verified)
- **Single write site.** All saves go through `WriteSaveData` → one `File.WriteAllText`. No separate files:
  the **thumbnail is base64-embedded inside the same JSON** (`wrapper.thumbnail`), so it's covered by the one
  flush.
- **No load-side change needed.** Unity's startup auto-syncs IndexedDB → memory (populate=true), so reads work
  once the data is in IndexedDB — which step 3 now guarantees.
- **No existing WebGL/jslib plugin** in v1 → this is purely additive; nothing to reconcile.

## Caveat (expected, not a bug)
`FS.syncfs` is **asynchronous** — the browser writes IndexedDB in the background. A refresh in the *same
instant* as a save could still race it, but a **normal reload survives**, which it didn't before. If you want
belt-and-suspenders later, the JS callback could message back into Unity to confirm the flush completed before
signaling "save done" — not needed for the fix.

## Verify
1. WebGL build → save in-game.
2. **Hard refresh (F5)** → load → the save is present. ✅
3. (Optional) Browser DevTools → **Application → IndexedDB → `/idbfs`** → the save bytes appear right after
   saving (they did not, before this fix).

## Reference
Identical fix in v2 (proven): commit `9070222` — `Assets/Narramancer/Unity/Plugins/WebGL/NarraSaveSync.jslib`
+ `Assets/Narramancer/Unity/Saving/NarraSaveSync.cs`. This v1 version is the same pattern, renamed to v1
conventions (no `Narra*` prefix) and simplified for v1's single write site.
