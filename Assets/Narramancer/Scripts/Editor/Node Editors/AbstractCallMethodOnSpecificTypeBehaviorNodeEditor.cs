
using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Narramancer {

	[CustomNodeEditor(typeof(CallMethodOnInstanceValueNode))]
	[CustomNodeEditor(typeof(CallMethodOnPropertyValueNode))]
	[CustomNodeEditor(typeof(CallMethodOnStatValueNode))]
	[CustomNodeEditor(typeof(CallMethodOnRelationshipValueNode))]
	public class AbstractCallMethodOnSpecificTypeBehaviorNodeEditor : NodeEditor {

		public override void OnBodyGUI() {

			var inputValuePort = target.GetInputPort("inputValue");
			var inputType = inputValuePort.ValueType;
			NodeEditorGUILayout.PortField( new GUIContent(ObjectNames.NicifyVariableName( inputType.Name)), inputValuePort, serializedObject);

			EditorGUILayout.Space(-EditorGUIUtility.singleLineHeight - EditorGUIUtility.standardVerticalSpacing);

			var passThroughValuePort = target.GetOutputPort("passThroughValue");
			NodeEditorGUILayout.PortField( passThroughValuePort, serializedObject);

			base.OnBodyGUI();
		}

	}
}