using System;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Narramancer {
	[CustomEditor(typeof(SetGlobalVariablesMonoBehaviour))]
	public class SetGlobalVariablesMonoBehaviourEditor : Editor {

		ReorderableList list;

		public override void OnInspectorGUI() {

			base.OnInspectorGUI();
			serializedObject.Update();

			var assignmentsProperty = serializedObject.FindProperty(SetGlobalVariablesMonoBehaviour.AssignmentsFieldName);

			if (list == null) {
				list = new ReorderableList(serializedObject, assignmentsProperty, true, true, true, true);
				list.drawElementCallback = DrawListItem;
				list.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Variable Assignments");
				list.onAddCallback = list => {
					var menu = new GenericMenu();

					foreach( var variable in NarramancerSingleton.Instance.GlobalVariables) {
						menu.AddItem( new GUIContent(variable.Name), false, () => {
							assignmentsProperty.InsertArrayElementAtIndex(assignmentsProperty.arraySize);
							var newElement = assignmentsProperty.GetArrayElementAtIndex(assignmentsProperty.arraySize - 1);
							newElement.FindPropertyRelative(nameof(VariableAssignment.name)).stringValue = variable.Name;
							newElement.FindPropertyRelative(nameof(VariableAssignment.id)).stringValue = variable.Id;
							newElement.FindPropertyRelative(nameof(VariableAssignment.type)).stringValue = VariableAssignment.TypeToString(variable.Type);
							serializedObject.ApplyModifiedProperties();
						});
					}

					menu.ShowAsContext();
				};
			}

			list.DoLayoutList();

			if (GUILayout.Button("Update Assignments")) {
				var runStoryTarget = target as SetGlobalVariablesMonoBehaviour;
				runStoryTarget.CreateInputs();
			}

			if (GUILayout.Button("Clear Assignments")) {
				assignmentsProperty.arraySize = 0;
			}

			if (GUILayout.Button("Apply Values")) {
				var runStoryTarget = target as SetGlobalVariablesMonoBehaviour;
				runStoryTarget.ApplyValues();
			}

			serializedObject.ApplyModifiedProperties();
		}

		private void DrawListItem(Rect rect, int index, bool isActive, bool isFocused) {
			var element = list.serializedProperty.GetArrayElementAtIndex(index);
			EditorDrawerUtilities.VariableAssignmentField(rect, element);
		}
	}
}