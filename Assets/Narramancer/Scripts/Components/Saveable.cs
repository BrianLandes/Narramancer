using System;
using System.Collections.Generic;
using System.Linq;
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
	/// <para>
	/// It is also what saves the object: the components ticked in its inspector, through their drivers
	/// (<see cref="IComponentSaver"/>), and every <see cref="SaveAttribute"/> field on its behaviour components.
	/// Added by hand, it saves everything it can; added by the migration tool purely for identity, it saves
	/// nothing until told to.
	/// </para>
	/// </summary>
	[DisallowMultipleComponent]
	[AddComponentMenu("Narramancer/Saveable")]
	public class Saveable : MonoBehaviour, ISerializableMonoBehaviour {

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
			SaveEverythingDetected();
		}

		private void OnValidate() {
			EnsureGuid();
		}

		private void Awake() {
			EnsureGuid();
			if (Application.isPlaying && !IsPrefabAsset()) {
				NarramancerSingleton.Instance.Register(this);
			}
		}

		private void OnDestroy() {
			if (HasGuid && claimed.TryGetValue(guid, out var holder) && holder == this) {
				claimed.Remove(guid);
			}
			if (Application.isPlaying) {
				NarramancerSingleton.Instance?.Unregister(this);
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

		#region Saving

		[SerializeField]
		[Tooltip("Save whether this object is active.")]
		private bool saveActive = false;

		[SerializeField]
		[Tooltip("The drivers, by key, whose components on this object are saved.")]
		private List<string> savedComponents = new List<string>();

		public bool SavesActive { get => saveActive; set => saveActive = value; }

		public bool Saves(IComponentSaver saver) => savedComponents.Contains(saver.Key);

		public void SetSaved(IComponentSaver saver, bool saved) {
			if (saved && !Saves(saver)) {
				savedComponents.Add(saver.Key);
			}
			else
			if (!saved) {
				savedComponents.Remove(saver.Key);
			}
		}

		/// <summary>Ticks every component that has a driver, and active state. What adding the component by hand does.</summary>
		public void SaveEverythingDetected() {
			saveActive = true;
			foreach (var (_, saver, _) in DetectedComponents()) {
				SetSaved(saver, true);
			}
		}

		/// <summary>Saves nothing but the guid. What the migration tool wants when it adds one only so the object can be named.</summary>
		public void SaveIdentityOnly() {
			saveActive = false;
			savedComponents.Clear();
		}

		/// <summary>
		/// Every component on this object that has a driver, with its index among components using the same
		/// driver — so two AudioSources on one object get two save entries rather than one overwriting the other.
		/// </summary>
		public IEnumerable<(Component component, IComponentSaver saver, int index)> DetectedComponents() {
			var counts = new Dictionary<IComponentSaver, int>();
			foreach (var component in GetComponents<Component>()) {
				if (component == null || !ComponentSaverRegistry.TryFind(component.GetType(), out var saver)) {
					continue;
				}
				counts.TryGetValue(saver, out var index);
				counts[saver] = index + 1;
				yield return (component, saver, index);
			}
		}

		/// <summary>
		/// The behaviour components whose <see cref="SaveAttribute"/> fields this saves. A component that saves
		/// itself — every <see cref="ISerializableMonoBehaviour"/>, the printers and verb runners among them — is
		/// left to do so, or its fields would be written twice.
		/// </summary>
		private IEnumerable<(MonoBehaviour behaviour, int index)> SavedBehaviours() {
			var counts = new Dictionary<Type, int>();
			foreach (var behaviour in GetComponents<MonoBehaviour>()) {
				if (behaviour == null || behaviour is ISerializableMonoBehaviour || !SavedFields.HasAny(behaviour.GetType())) {
					continue;
				}
				var type = behaviour.GetType();
				counts.TryGetValue(type, out var index);
				counts[type] = index + 1;
				yield return (behaviour, index);
			}
		}

		private string Key(string name) => $"{EnsureGuid()}:{name}";

		private static string Slot(string name, int index) => index == 0 ? name : $"{name}#{index}";

		private const string SavedMarker = "$saved";
		private const string ActiveKey = "$active";
		private const string BehavioursKey = "$behaviours";

		public void Serialize(StoryInstance story) {
			story.SaveTable.SetBool(Key(SavedMarker), true);

			foreach (var (component, saver, index) in DetectedComponents()) {
				if (Saves(saver)) {
					story.SaveTable.Set(Key(Slot(saver.Key, index)), saver.Capture(component), saver.StateType);
				}
			}

			// Which behaviours were saved, by type, so a load can re-add any that were added at runtime — a
			// Tweener a node attached mid-story is not in the scene file, and its state has nowhere to go otherwise.
			var behaviourTypes = new List<string>();
			foreach (var (behaviour, index) in SavedBehaviours()) {
				var name = Slot(behaviour.GetType().Name, index);
				SavedFields.Capture(behaviour, field => Key($"{name}:{field}"), story);
				behaviourTypes.Add(Serialization.TypeNames.Write(behaviour.GetType()));
			}
			if (behaviourTypes.Count > 0) {
				story.SaveTable.SetObject(Key(BehavioursKey), behaviourTypes);
			}

			if (saveActive) {
				story.SaveTable.SetBool(Key(ActiveKey), gameObject.activeSelf);
			}
		}

		public void Deserialize(StoryInstance story) {
			if (!story.SaveTable.HasBool(Key(SavedMarker))) {
				// The save knew nothing of this object — it was added since. Leave it as the scene built it.
				return;
			}
			story.SaveTable.RemoveBool(Key(SavedMarker));

			foreach (var (component, saver, index) in DetectedComponents().ToList()) {
				var key = Key(Slot(saver.Key, index));
				if (Saves(saver) && story.SaveTable.Contains(key, saver.StateType)) {
					saver.Apply(component, story.SaveTable.GetAndRemove(key, saver.StateType));
				}
			}

			if (story.SaveTable.GetObject(Key(BehavioursKey)) is List<string> behaviourTypes) {
				story.SaveTable.RemoveObject(Key(BehavioursKey));
				RestoreMissingBehaviours(behaviourTypes);
			}
			foreach (var (behaviour, index) in SavedBehaviours().ToList()) {
				var name = Slot(behaviour.GetType().Name, index);
				SavedFields.Restore(behaviour, field => Key($"{name}:{field}"), story);
			}

			// Last, because activating wakes the object, and its components should wake into their saved state.
			if (story.SaveTable.HasBool(Key(ActiveKey))) {
				gameObject.SetActive(story.SaveTable.GetBool(Key(ActiveKey)));
				story.SaveTable.RemoveBool(Key(ActiveKey));
			}
		}

		private void RestoreMissingBehaviours(List<string> typeNames) {
			// Counted, not just checked for presence: two Tweeners saved means two wanted.
			foreach (var group in typeNames.GroupBy(name => name)) {
				if (!Serialization.TypeNames.TryRead(group.Key, out var type) || !typeof(MonoBehaviour).IsAssignableFrom(type)) {
					Debug.LogWarning($"'{name}' was saved with a '{group.Key}' component that this build does not have, so its state cannot be restored.", this);
					continue;
				}
				var missing = group.Count() - GetComponents(type).Length;
				for (var i = 0; i < missing; i++) {
					gameObject.AddComponent(type);
				}
			}
		}

		#endregion
	}
}
