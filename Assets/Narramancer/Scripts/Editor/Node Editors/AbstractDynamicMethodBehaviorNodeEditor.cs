
//using UnityEditor;
//using UnityEngine;
//using XNodeEditor;

//namespace Narramancer {

//	[CustomNodeEditor(typeof(AbstractDynamicMethodBehaviorNode))]
//	public class AbstractDynamicMethodBehaviorNodeEditor : NodeEditor {

//		public override void OnBodyGUI() {

//			serializedObject.Update();

//			var nounTypeProperty = serializedObject.FindProperty(IAbstractInstanceInputNode.NounTypeField);
//			var nounType = (NounAssignmentType)nounTypeProperty.intValue;

//			switch (nounType) {
//				case NounAssignmentType.Instance:
					
//					var instancePort = target.GetInputPort(IAbstractInstanceInputNode.NounInstanceField);
//					NodeEditorGUILayout.PortField(GUIContent.none, instancePort, serializedObject);

//					EditorGUILayout.Space(-EditorGUIUtility.singleLineHeight - EditorGUIUtility.standardVerticalSpacing);

//					EditorGUILayout.PropertyField(nounTypeProperty, GUIContent.none);

//					break;
//				case NounAssignmentType.Predefined:
//					EditorGUILayout.BeginHorizontal();

//					var scriptableObjectPort = target.GetInputPort(IAbstractInstanceInputNode.NounScriptableObjectField);
//					NodeEditorGUILayout.PortField(GUIContent.none, scriptableObjectPort, serializedObject, GUILayout.Width(85) );
					
//					EditorGUILayout.PropertyField(nounTypeProperty, GUIContent.none);

//					EditorGUILayout.EndHorizontal();
//					break;
//			}

//			EditorGUILayout.Space(-EditorGUIUtility.singleLineHeight - EditorGUIUtility.standardVerticalSpacing);

//			var passThroughPort = target.GetOutputPort(IAbstractInstanceInputNode.PassThroughInstanceField);
//			NodeEditorGUILayout.PortField(GUIContent.none, passThroughPort, serializedObject);

//			serializedObject.ApplyModifiedProperties();

//			base.OnBodyGUI();
//		}

//	}
//}