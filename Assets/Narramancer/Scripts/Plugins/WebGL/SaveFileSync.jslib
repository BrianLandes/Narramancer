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
