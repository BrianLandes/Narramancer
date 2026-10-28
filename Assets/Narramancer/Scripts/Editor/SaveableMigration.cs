using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Narramancer {

	/// <summary>
	/// Adds a <see cref="Saveable"/> to every object that saves state but has not got one yet.
	/// <para>
	/// Needed because save identity moved from the object's scene path to a minted guid held by
	/// <see cref="Saveable"/>. Anything with a <see cref="SerializableMonoBehaviour"/> and no
	/// <see cref="Saveable"/> has no durable identity, and says so at save time — this finds them all at once
	/// instead.
	/// </para>
	/// <para>
	/// Also the upgrade path for a project built against an older version, which is why it reports rather than
	/// silently fixing: someone running it on their own game wants to see what changed.
	/// </para>
	/// </summary>
	public static class SaveableMigration {

		[MenuItem("Window/Narramancer/Add Missing Saveables (open scenes)", priority = 100)]
		public static void AddMissingInOpenScenes() {
			var added = new List<string>();
			var minted = new List<string>();
			var alreadyFine = 0;

			for (var i = 0; i < SceneManager.sceneCount; i++) {
				var scene = SceneManager.GetSceneAt(i);
				if (!scene.isLoaded) {
					continue;
				}

				foreach (var root in scene.GetRootGameObjects()) {
					foreach (var saver in root.GetComponentsInChildren<SerializableMonoBehaviour>(true)) {

						var saveable = saver.GetComponent<Saveable>();
						if (saveable == null) {
							saveable = Undo.AddComponent<Saveable>(saver.gameObject);
							// Identity only: the object's existing savers already save what it needs, and Reset —
							// which AddComponent fires — would otherwise tick every component it can see.
							saveable.SaveIdentityOnly();
							added.Add($"{scene.name}: {Path(saver.transform)} ({saver.GetType().Name})");
						}
						else {
							alreadyFine++;
						}

						// Minted even when the component was already there. A scene instance of a prefab
						// inherits the component but not an id — the prefab asset deliberately has none — so
						// "it already has a Saveable" does not mean "it already has an identity".
						var before = saveable.Guid;
						if (saveable.EnsureGuid() != before) {
							minted.Add($"{scene.name}: {Path(saver.transform)}");
						}
						EditorUtility.SetDirty(saver.gameObject);
					}
				}

				if (added.Count > 0 || minted.Count > 0) {
					EditorSceneManager.MarkSceneDirty(scene);
				}
			}

			AddMissingForWiredUpObjects(added, minted);

			Report("open scenes", added, alreadyFine);
			if (minted.Count > 0) {
				Debug.Log($"Narramancer: minted {minted.Count} id(s) for objects that had a Saveable but no identity yet.\n  "
					+ string.Join("\n  ", minted));
			}
		}

		/// <summary>
		/// The same pass over prefabs. Separate from the scene pass because a prefab is edited and saved
		/// differently, and because a prefab asset deliberately gets the component but <b>no id</b> — every
		/// instance would otherwise answer to the same one. Instances mint their own on placement.
		/// </summary>
		[MenuItem("Window/Narramancer/Add Missing Saveables (prefabs)", priority = 101)]
		public static void AddMissingInPrefabs() {
			var added = new List<string>();
			var alreadyFine = 0;

			foreach (var guid in AssetDatabase.FindAssets("t:Prefab")) {
				var path = AssetDatabase.GUIDToAssetPath(guid);
				if (!path.StartsWith("Assets/", System.StringComparison.Ordinal)) {
					continue;
				}

				var prefab = PrefabUtility.LoadPrefabContents(path);
				var changed = false;
				try {
					foreach (var saver in prefab.GetComponentsInChildren<SerializableMonoBehaviour>(true)) {
						if (saver.GetComponent<Saveable>() != null) {
							alreadyFine++;
							continue;
						}
						// Deliberately not EnsureGuid: the asset must stay id-less.
						saver.gameObject.AddComponent<Saveable>().SaveIdentityOnly();
						changed = true;
						added.Add($"{System.IO.Path.GetFileName(path)}: {Path(saver.transform)} ({saver.GetType().Name})");
					}

					if (changed) {
						PrefabUtility.SaveAsPrefabAsset(prefab, path);
					}
				}
				finally {
					PrefabUtility.UnloadPrefabContents(prefab);
				}
			}

			Report("prefabs", added, alreadyFine);
		}

		/// <summary>
		/// Adds a <see cref="Saveable"/> to scene objects a verb has been <b>wired to</b>, which the scan above
		/// cannot see.
		/// <para>
		/// A UI <c>Text</c> that a verb writes into carries no <see cref="SerializableMonoBehaviour"/> of its
		/// own — nothing about the object says "I am saved". What connects it is a
		/// <see cref="VariableAssignment"/> on whatever runs the verb, so that is what gets searched. Without
		/// this the reference is dropped at save time and the author only finds out from an error, which is how
		/// the Character Equipment sample lost its labels.
		/// </para>
		/// </summary>
		private static void AddMissingForWiredUpObjects(List<string> added, List<string> minted) {
			for (var i = 0; i < SceneManager.sceneCount; i++) {
				var scene = SceneManager.GetSceneAt(i);
				if (!scene.isLoaded) {
					continue;
				}

				var changed = false;

				foreach (var root in scene.GetRootGameObjects()) {
					foreach (var behaviour in root.GetComponentsInChildren<MonoBehaviour>(true)) {
						if (behaviour == null) {
							continue;
						}

						foreach (var target in AssignedObjectsOn(behaviour)) {
							if (!Serialization.SceneObjectIds.IsSceneObject(target)) {
								continue;
							}

							var targetObject = target as GameObject ?? (target as Component)?.gameObject;
							if (targetObject == null) {
								continue;
							}

							var saveable = targetObject.GetComponent<Saveable>();
							if (saveable == null) {
								saveable = Undo.AddComponent<Saveable>(targetObject);
								// Wired up so a save can name it, not so its transform starts being restored.
								saveable.SaveIdentityOnly();
								added.Add($"{scene.name}: {Path(targetObject.transform)} (wired to {behaviour.GetType().Name} on {behaviour.gameObject.name})");
								changed = true;
							}

							var before = saveable.Guid;
							if (saveable.EnsureGuid() != before) {
								minted.Add($"{scene.name}: {Path(targetObject.transform)}");
								changed = true;
							}
							EditorUtility.SetDirty(targetObject);
						}
					}
				}

				if (changed) {
					EditorSceneManager.MarkSceneDirty(scene);
				}
			}
		}

		/// <summary>
		/// Every object a component assigns to a variable, at whatever depth the assignment is nested.
		/// <para>
		/// Asked of Unity through a <see cref="SerializedObject"/> rather than walked by hand. The first version
		/// of this reflected over the component's own fields looking for a <see cref="VariableAssignment"/> or a
		/// list of them, and found none in the Character Equipment sample: the assignments there are three levels
		/// down — <c>NarramancerScene.variables</c> holds a list wrapper, which holds
		/// <see cref="NarramancerPortWithAssignment"/>, which holds the assignment. A hand-rolled walk has to
		/// re-derive Unity's rules about which fields it descends into and how deep; iterating the serialized
		/// data gets nesting, lists, wrappers and base classes for free, and cannot disagree with what Unity
		/// actually stored.
		/// </para>
		/// </summary>
		public static IEnumerable<Object> AssignedObjectsOn(MonoBehaviour behaviour) {
			using (var serialized = new SerializedObject(behaviour)) {
				var property = serialized.GetIterator();
				while (property.Next(true)) {
					if (property.propertyType != SerializedPropertyType.Generic
						|| !string.Equals(property.type, nameof(VariableAssignment), System.StringComparison.Ordinal)) {
						continue;
					}

					var value = property.FindPropertyRelative(nameof(VariableAssignment.objectValue));
					if (value != null && value.objectReferenceValue != null) {
						yield return value.objectReferenceValue;
					}
				}
			}
		}

		private static string Path(Transform transform) {
			var parts = new List<string>();
			for (var current = transform; current != null; current = current.parent) {
				parts.Add(current.name);
			}
			parts.Reverse();
			return string.Join("/", parts);
		}

		private static void Report(string where, List<string> added, int alreadyFine) {
			if (added.Count == 0) {
				Debug.Log($"Narramancer: every object that saves state in {where} already has a Saveable ({alreadyFine} checked).");
				return;
			}

			Debug.Log(
				$"Narramancer: added {added.Count} Saveable component(s) across {where} ({alreadyFine} already had one)."
				+ " Save the scenes to keep the minted ids.\n  " + string.Join("\n  ", added));
		}
	}
}
