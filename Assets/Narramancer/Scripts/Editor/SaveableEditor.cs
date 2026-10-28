using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Narramancer {

	/// <summary>
	/// Shows what a <see cref="Saveable"/> will save: a toggle for active state and for each component that has a
	/// driver, and — read-only, since the attribute decides — the behaviour components whose
	/// <see cref="SaveAttribute"/> fields come along.
	/// </summary>
	[CustomEditor(typeof(Saveable))]
	public class SaveableEditor : Editor {

		public override void OnInspectorGUI() {
			var saveable = (Saveable)target;

			using (new EditorGUI.DisabledScope(true)) {
				EditorGUILayout.TextField(new GUIContent("Id", "Identifies this object in save files. Changing it orphans existing saves."),
					saveable.HasGuid ? saveable.Guid : "(none — minted when placed in a scene)");
			}

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Saves", EditorStyles.boldLabel);

			EditorGUI.BeginChangeCheck();
			var savesActive = EditorGUILayout.ToggleLeft("Active state", saveable.SavesActive);
			if (EditorGUI.EndChangeCheck()) {
				Undo.RecordObject(saveable, "Change what is saved");
				saveable.SavesActive = savesActive;
				EditorUtility.SetDirty(saveable);
			}

			var detected = saveable.DetectedComponents().ToList();
			foreach (var (component, saver, index) in detected) {
				// One toggle per driver: two AudioSources are saved or not together.
				if (index > 0) {
					continue;
				}
				var count = detected.Count(entry => entry.saver == saver);
				var label = count > 1 ? $"{ObjectNames.NicifyVariableName(component.GetType().Name)} (×{count})" : ObjectNames.NicifyVariableName(component.GetType().Name);

				EditorGUI.BeginChangeCheck();
				var saved = EditorGUILayout.ToggleLeft(label, saveable.Saves(saver));
				if (EditorGUI.EndChangeCheck()) {
					Undo.RecordObject(saveable, "Change what is saved");
					saveable.SetSaved(saver, saved);
					EditorUtility.SetDirty(saveable);
				}
			}

			var behaviours = saveable.GetComponents<MonoBehaviour>()
				.Where(behaviour => behaviour != null && !(behaviour is ISerializableMonoBehaviour) && SavedFields.HasAny(behaviour.GetType()))
				.ToList();
			if (behaviours.Count > 0) {
				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Also saved, by their [Save] fields", EditorStyles.miniBoldLabel);
				foreach (var behaviour in behaviours) {
					EditorGUILayout.LabelField("    " + ObjectNames.NicifyVariableName(behaviour.GetType().Name), EditorStyles.miniLabel);
				}
			}

			if (!saveable.SavesActive && !detected.Any(entry => saveable.Saves(entry.saver)) && behaviours.Count == 0) {
				EditorGUILayout.HelpBox("Identity only: other saves can point at this object, but nothing about it is saved.", MessageType.None);
			}
		}
	}
}
