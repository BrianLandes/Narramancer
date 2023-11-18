
using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Narramancer {

	[CustomNodeEditor(typeof(AbstractInstanceInputRunnableNode))]
	public class AbstractInstanceInputRunnableNodeEditor : RunnableNodeEditor {

		public override void OnBodyGUI() {
			OnTopGUI();

			serializedObject.Update();

			var nounTypeProperty = serializedObject.FindProperty(AbstractInstanceInputNode.NounTypeField);
			var nounType = (InstanceAssignmentType)nounTypeProperty.intValue;

			switch (nounType) {
				case InstanceAssignmentType.Instance:

					var instancePort = target.GetInputPort(AbstractInstanceInputNode.NounInstanceField);
					NodeEditorGUILayout.PortField(GUIContent.none, instancePort, serializedObject);

					EditorGUILayout.Space(-EditorGUIUtility.singleLineHeight - EditorGUIUtility.standardVerticalSpacing);

					EditorGUILayout.PropertyField(nounTypeProperty, GUIContent.none);

					break;
				case InstanceAssignmentType.ScriptableObject:

					EditorGUILayout.BeginHorizontal();
					Rect rect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight);
					var position = rect.position - new Vector2(16, 0);

					var scriptableObjectPort = target.GetInputPort(AbstractInstanceInputNode.NounScriptableObjectField);

					NodeEditorGUILayout.PortField(position, scriptableObjectPort);

					var nounScriptableObjectPropert = serializedObject.FindProperty(AbstractInstanceInputNode.NounScriptableObjectField);
					EditorGUILayout.PropertyField(nounScriptableObjectPropert, GUIContent.none, GUILayout.Width(85));


					EditorGUILayout.PropertyField(nounTypeProperty, GUIContent.none);

					EditorGUILayout.EndHorizontal();
					break;
			}

			EditorGUILayout.Space(-EditorGUIUtility.singleLineHeight - EditorGUIUtility.standardVerticalSpacing);

			var passThroughPort = target.GetOutputPort(AbstractInstanceInputNode.PassThroughInstanceField);
			NodeEditorGUILayout.PortField(GUIContent.none, passThroughPort, serializedObject);

			EditorGUILayout.Space( EditorGUIUtility.standardVerticalSpacing);

			serializedObject.ApplyModifiedProperties();

			OnBaseBodyGUI();
		}

	}
}