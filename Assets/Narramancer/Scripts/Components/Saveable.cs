using System.Collections.Generic;
using UnityEngine;

namespace Narramancer {

	/// <summary>
	/// Gives a GameObject a durable identity, so the state saved against it can be found again.
	/// <para>
	/// Before this, a saved value was keyed by <c>"{field} {transform.FullPath()}[{componentIndex}]"</c>.
	/// Renaming the object, reparenting it, reordering its components, or giving two siblings the same name
	/// all changed that key — and a key that no longer matches does not error, it reads back as the default.
	/// The save loaded, and the object was quietly wrong. A minted guid changes only when someone changes it.
	/// </para>
	/// <para>
	/// One per GameObject. Every <see cref="SerializableMonoBehaviour"/> on the object keys its state off this
	/// component's <see cref="Guid"/>, so they share one identity rather than each inventing their own.
	/// </para>
	/// </summary>
	[DisallowMultipleComponent]
	[AddComponentMenu("Narramancer/Saveable")]
	public class Saveable : MonoBehaviour {

		[SerializeField]
		[Tooltip("Identifies this object in save files. Minted automatically; changing it orphans existing saves.")]
		private string guid = string.Empty;

		/// <summary>This object's durable id. Empty only if minting has not happened yet.</summary>
		public string Guid => guid;

		public bool HasGuid => !guid.IsNullOrEmpty();

		/// <summary>
		/// Who currently holds each guid, so a duplicate can be spotted in O(1) rather than by scanning the
		/// scene on every inspector change.
		/// </summary>
		private static readonly Dictionary<string, Saveable> claimed = new Dictionary<string, Saveable>();

		private static string Mint() => System.Guid.NewGuid().ToString("N");

		/// <summary>
		/// This object's id, minting one if it has none and re-minting if another live object already answers to
		/// it. Idempotent, and safe to call from anywhere.
		/// <para>
		/// The work lives here rather than only in <c>Reset</c>/<c>OnValidate</c>/<c>Awake</c> because those are
		/// Unity's to call, not ours: none of them run for an object built in an EditMode test, and
		/// <c>Instantiate</c> does not run <c>Awake</c> outside play mode. Identity that only exists if a
		/// lifecycle callback happened to fire is identity that cannot be tested and can be missed. The callbacks
		/// still call this — they just make it happen <em>early</em>, so the guid is serialized at author time
		/// rather than first save.
		/// </para>
		/// </summary>
		public string EnsureGuid() {

			// A prefab *asset* must never carry an id — every instance made from it would answer to the same one.
			// It can acquire one by accident: saving a scene object as a prefab copies whatever guid it had. So
			// this clears it rather than merely declining to mint, and each instance takes its own.
			if (IsPrefabAsset()) {
				if (HasGuid) {
					guid = string.Empty;
#if UNITY_EDITOR
					UnityEditor.EditorUtility.SetDirty(this);
#endif
				}
				return string.Empty;
			}

			var minted = false;

			if (!HasGuid) {
				guid = Mint();
				minted = true;
			}
			else if (claimed.TryGetValue(guid, out var holder) && holder != null && holder != this) {
				// Someone else already answers to this id — a copy-paste, most likely, which copies the
				// serialized guid. Whoever arrives second takes a new one, so the original keeps the id its
				// existing saves refer to.
				guid = Mint();
				minted = true;
			}

			Claim();

#if UNITY_EDITOR
			// Persist it, or the guid is re-minted every session and nothing it keys ever matches.
			if (minted && !Application.isPlaying) {
				UnityEditor.EditorUtility.SetDirty(this);
			}
#endif
			return guid;
		}

		/// <summary>Called when the component is first added, which is the normal way an object gets its id.</summary>
		private void Reset() {
			EnsureGuid();
		}

		private void OnValidate() {
			EnsureGuid();
		}

		private void Awake() {
			EnsureGuid();
		}

		private void OnDestroy() {
			if (HasGuid && claimed.TryGetValue(guid, out var holder) && holder == this) {
				claimed.Remove(guid);
			}
		}

		/// <summary>
		/// True when this lives in an asset rather than a scene — a prefab on disk, or one open in Prefab Mode.
		/// Such an object is a template, not a thing in the world, so it has no identity of its own.
		/// </summary>
		private bool IsPrefabAsset() {
#if UNITY_EDITOR
			if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(this)) {
				return true;
			}
			if (UnityEditor.SceneManagement.PrefabStageUtility.GetPrefabStage(gameObject) != null) {
				return true;
			}
#endif
			// Also covers a prefab loaded at runtime: it is not in a scene until something instantiates it.
			return !gameObject.scene.IsValid();
		}

		private void Claim() {
			if (HasGuid) {
				claimed[guid] = this;
			}
		}

		/// <summary>
		/// Forces this object's id, for a runtime-spawned object being restored from a save. The spawner
		/// records the guid it gave each spawn and puts it back on the replacement, because a fresh mint
		/// would not match the state the save is holding for it.
		/// </summary>
		public void AssignGuid(string value) {
			if (value.IsNullOrEmpty()) {
				return;
			}
			if (HasGuid && claimed.TryGetValue(guid, out var holder) && holder == this) {
				claimed.Remove(guid);
			}
			guid = value;
			Claim();
		}

		/// <summary>
		/// The live object answering to <paramref name="guid"/>, if one is loaded. This is how a save points at a
		/// scene object: the id names the guid, and the object that currently holds it is found here.
		/// </summary>
		public static bool TryFind(string guid, out Saveable saveable) {
			saveable = null;
			if (guid.IsNullOrEmpty()) {
				return false;
			}
			if (claimed.TryGetValue(guid, out saveable) && saveable != null) {
				return true;
			}

			// Claims are made in Awake, and Unity never calls Awake on an object that starts inactive — a hidden
			// panel, a door not yet opened — so such an object answers to nothing until it is first shown, and a
			// save pointing at it fails to load. Look for it directly before giving up, claiming what turns up.
			// Only reached on a miss, so the scan costs nothing on the normal path.
			ClaimAllInLoadedScenes();
			if (claimed.TryGetValue(guid, out saveable) && saveable != null) {
				return true;
			}
			saveable = null;
			return false;
		}

		private static void ClaimAllInLoadedScenes() {
			for (var i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++) {
				var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
				if (!scene.isLoaded) {
					continue;
				}
				foreach (var root in scene.GetRootGameObjects()) {
					foreach (var saveable in root.GetComponentsInChildren<Saveable>(true)) {
						if (saveable.HasGuid && !claimed.ContainsKey(saveable.Guid)) {
							saveable.Claim();
						}
					}
				}
			}
		}

		/// <summary>The <see cref="Saveable"/> on this object, or null with an error naming what is missing.</summary>
		public static Saveable Of(Component component) {
			var saveable = component.GetComponent<Saveable>();
			if (saveable == null) {
				Debug.LogError(
					$"'{component.gameObject.name}' has a {component.GetType().Name} but no {nameof(Saveable)}, so its state cannot be saved or restored. " +
					$"Add a {nameof(Saveable)} component to it.", component);
			}
			return saveable;
		}

		/// <summary>
		/// Forgets which objects currently hold which guids. Only useful to tests and tooling that create and
		/// destroy Saveables outside the normal scene lifecycle; the table maintains itself otherwise.
		/// </summary>
		public static void ClearClaims() => claimed.Clear();
	}
}
