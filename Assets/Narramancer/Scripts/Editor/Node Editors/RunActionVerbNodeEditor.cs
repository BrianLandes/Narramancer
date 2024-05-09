
using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Narramancer {
	[CustomNodeEditor(typeof(RunActionVerbNode))]
	public class RunActionVerbNodeEditor : ChainedRunnableNodeEditor {

		public override void OnBodyGUI() {
			OnTopGUI();

			var runActionVerbNode = target as RunActionVerbNode;
			var ioGraph = runActionVerbNode.actionVerb as VerbGraph;
			if (ioGraph != null) {
				var needsUpdate = !runActionVerbNode.HasMatchingNodeInputsAndOutputsForGraphInputsAndOutputs(ioGraph);
				if (needsUpdate) {
					runActionVerbNode.UpdatePorts();
				}
			}

			var actionVerbPort = target.GetPort(nameof(RunActionVerbNode.actionVerb));
			var label = actionVerbPort.IsConnected ? new GUIContent("Action Verb") : GUIContent.none;
			NodeEditorGUILayout.PortField(label, actionVerbPort, serializedObject);

			if (!actionVerbPort.IsConnected) {
				EditorGUILayout.Space(-EditorGUIUtility.singleLineHeight - EditorGUIUtility.standardVerticalSpacing);

				var actionVerbProperty = serializedObject.FindProperty(nameof(RunActionVerbNode.actionVerb));
				EditorGUILayout.PropertyField(actionVerbProperty);
			}

			OnBaseBodyGUI();
		}
	}
}