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
						saver.gameObject.AddComponent<Saveable>();
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
