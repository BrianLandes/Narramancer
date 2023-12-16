using UnityEditor;
using UnityEngine;

namespace Narramancer {

	[CustomNodeEditor(typeof(ChooseRankedWeightedActionNode))]
	public class ChooseRankedWeightedActionNodeEditor : AbstractInstanceInputChainedRunnableNodeEditor {

		public override void OnBodyGUI() {
			base.OnBodyGUI();

			if (GUILayout.Button("Create Child Action")) {

				var targetNode = target as ChooseRankedWeightedActionNode;
				targetNode.CreateChildAction();
			}

			var logProperty = serializedObject.FindProperty(ChooseRankedWeightedActionNode.LogFieldName);
			if ( logProperty.stringValue.IsNotNullOrEmpty() ) {
				logProperty.isExpanded = EditorGUILayout.Foldout(logProperty.isExpanded, "Log");
				if (logProperty.isExpanded) {
					EditorGUILayout.HelpBox(logProperty.stringValue, MessageType.None);
				}
			}
		}
	}
}