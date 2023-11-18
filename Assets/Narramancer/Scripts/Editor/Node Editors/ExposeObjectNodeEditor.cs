using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Narramancer {

	[CustomNodeEditor(typeof(ExposeObjectNode))]
	public class ExposeObjectNodeEditor : NodeEditor {

		public override void OnBodyGUI() {
			serializedObject.Update();

			ShowOrHideButton();

			base.OnBodyGUI();

			ShowOrHideButton();
		}

		private void ShowOrHideButton() {
			var showAllFieldsProperty = serializedObject.FindProperty("showAllFields");
			if (showAllFieldsProperty.boolValue) {
				if (GUILayout.Button("Hide Unused")) {
					showAllFieldsProperty.boolValue = false;
					serializedObject.ApplyModifiedProperties();
					target.UpdatePorts();
				}
			}
			else {
				if (GUILayout.Button("Show All")) {
					showAllFieldsProperty.boolValue = true;
					serializedObject.ApplyModifiedProperties();
					target.UpdatePorts();
				}
			}
		}
	}
}