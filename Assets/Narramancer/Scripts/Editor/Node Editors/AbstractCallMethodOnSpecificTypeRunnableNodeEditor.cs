
using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Narramancer {

	[CustomNodeEditor(typeof(CallMethodOnInstanceRunnableNode))]
	[CustomNodeEditor(typeof(CallMethodOnPropertyRunnableNode))]
	[CustomNodeEditor(typeof(CallMethodOnStatRunnableNode))]
	[CustomNodeEditor(typeof(CallMethodOnRelationshipRunnableNode))]
	public class AbstractCallMethodOnSpecificTypeRunnableNodeEditor : ChainedRunnableNodeEditor {

		public override void OnBodyGUI() {

			OnTopGUI();

			var inputValuePort = target.GetInputPort("inputValue");
			var inputType = inputValuePort.ValueType;
			NodeEditorGUILayout.PortField( new GUIContent(ObjectNames.NicifyVariableName( inputType.Name)), inputValuePort, serializedObject);

			EditorGUILayout.Space(-EditorGUIUtility.singleLineHeight - EditorGUIUtility.standardVerticalSpacing);

			var passThroughValuePort = target.GetOutputPort("passThroughValue");
			NodeEditorGUILayout.PortField( passThroughValuePort, serializedObject);

			OnBaseBodyGUI();
		}

	}
}