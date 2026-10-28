using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Narramancer {
	[CustomEditor(typeof(RunActionVerbMonoBehaviour))]
	public class RunActionVerbMonoBehaviourEditor : Editor {

		ReorderableList list;

		public override void OnInspectorGUI() {

			base.OnInspectorGUI();
			serializedObject.Update();

			var assignmentsProperty = serializedObject.FindProperty(RunActionVerbMonoBehaviour.AssignmentsFieldName);

			if (list == null) {
				list = new ReorderableList(serializedObject, assignmentsProperty, true, true, true, true);
				list.drawElementCallback = DrawListItem;
				list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Variable Assignments");
			}

			list.DoLayoutList();

			if (GUILayout.Button("Update Assignments")) {
				var runGraphTarget = target as RunActionVerbMonoBehaviour;
				// Recorded, so the change dirties the scene (and saves) and can be undone; then re-read, so the list
				// draws what CreateInputs left rather than this frame's stale copy.
				Undo.RecordObject(runGraphTarget, "Update Assignments");
				runGraphTarget.CreateInputs();
				serializedObject.Update();
			}

			if (GUILayout.Button("Clear Assignments")) {
				assignmentsProperty.arraySize = 0;
			}

			if (GUILayout.Button("Run Verb")) {
				var runGraphTarget = target as RunActionVerbMonoBehaviour;
				runGraphTarget.RunVerb();
			}

			serializedObject.ApplyModifiedProperties();
		}

		private void DrawListItem(Rect rect, int index, bool isActive, bool isFocused) {
			var element = list.serializedProperty.GetArrayElementAtIndex(index);
			EditorDrawerUtilities.VariableAssignmentField(rect, element);
			
		}
	}
}