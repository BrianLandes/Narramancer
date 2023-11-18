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
				runGraphTarget.CreateInputs();
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