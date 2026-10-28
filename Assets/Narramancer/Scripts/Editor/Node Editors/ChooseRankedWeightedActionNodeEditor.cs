using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Narramancer {

	[CustomNodeEditor(typeof(ChooseRankedWeightedActionNode))]
	public class ChooseRankedWeightedActionNodeEditor : AbstractInstanceInputChainedRunnableNodeEditor {

		public override void OnBodyGUI() {
			// The actions are separate assets, so editing one's effect graph never reaches this node by itself.
			// Refreshed on Layout only: changing ports between Layout and Repaint breaks IMGUI's control matching.
			if (Event.current.type == EventType.Layout) {
				var targetNode = target as ChooseRankedWeightedActionNode;
				var before = PortSignature(targetNode);
				targetNode.UpdatePorts();
				if (PortSignature(targetNode) != before) {
					EditorUtility.SetDirty(targetNode);
				}
			}

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

		private static string PortSignature(ChooseRankedWeightedActionNode node) {
			return string.Join("|", node.DynamicInputs.Select(port => port.fieldName + ":" + port.ValueType?.AssemblyQualifiedName));
		}
	}
}