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
