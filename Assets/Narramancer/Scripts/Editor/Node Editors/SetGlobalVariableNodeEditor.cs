//using System;
//using System.Linq;
//using UnityEditor;
//using UnityEngine;
//using XNodeEditor;

//namespace Narramancer {
//	[CustomNodeEditor(typeof(SetGlobalVariableNode))]
//	public class SetGlobalVariableNodeEditor : ChainedRunnableNodeEditor {

//		public override void OnBodyGUI() {

//			base.OnTopGUI();

//			var setStoryVariableNode =  target as SetGlobalVariableNode;

//			serializedObject.Update();

//			var variables = NarramancerSingleton.Instance.GlobalVariables;

//			var outputId = serializedObject.FindProperty("variableId");
//			if (outputId.stringValue.IsNullOrEmpty() && variables.Any()) {
//				outputId.stringValue = variables.First().Id;
//				serializedObject.ApplyModifiedProperties();

//				setStoryVariableNode.ApplyChanges();
//			}

//			var buttonText = string.Empty;

//			var correspondingOutput = variables.FirstOrDefault(output => output.Id.Equals(outputId.stringValue));
//			if (correspondingOutput != null) {

//				buttonText = correspondingOutput.ToString();
//			}
//			else {
//				buttonText = "(None)";
//			}

//			var originalColor = GUI.color;

//			var nodePort = correspondingOutput != null ? setStoryVariableNode.GetInputPort(SetGlobalVariableNode.PORT_NAME) : null;

//			if (nodePort != null) {
//				GUI.color = NodeEditorPreferences.GetTypeColor(nodePort.ValueType);
//			}

//			if (EditorGUILayout.DropdownButton(new GUIContent(buttonText, buttonText), FocusType.Passive)) {
//				GenericMenu context = new GenericMenu();

//				foreach (var output in variables) {
//					context.AddItem(new GUIContent(output.ToString()), output.Id == outputId.stringValue, () => {
//						serializedObject.Update();
//						correspondingOutput = output;
//						outputId.stringValue = correspondingOutput.Id;
//						serializedObject.ApplyModifiedProperties();

//						setStoryVariableNode.ApplyChanges();
//					});
//				}

//				if (context.GetItemCount() == 0) {
//					context.AddDisabledItem(new GUIContent("(No valid values)"));
//				}

//				Matrix4x4 originalMatrix = GUI.matrix;
//				GUI.matrix = Matrix4x4.identity;
//				context.ShowAsContext();
//				GUI.matrix = originalMatrix;

//			}

//			GUI.color = originalColor;

//			if (nodePort != null) {

//				bool IsTypeThatCanShowBackend(Type type) {
//					return typeof(bool).IsAssignableFrom(type) || typeof(string).IsAssignableFrom(type) || typeof(int).IsAssignableFrom(type) || typeof(float).IsAssignableFrom(type) || typeof(UnityEngine.Object).IsAssignableFrom(type);
//				}

//				if (IsTypeThatCanShowBackend(nodePort.ValueType) && !nodePort.IsConnected) {
//					NodeEditorGUILayout.PortField(GUIContent.none, nodePort, serializedObject);
//				}
//				else {
//					GUILayout.Space(-EditorGUIUtility.singleLineHeight);
//					NodeEditorGUILayout.PortField(GUIContent.none, nodePort, serializedObject);
//				}

//			}

//			serializedObject.ApplyModifiedProperties();

//			if (correspondingOutput != null && (nodePort == null || nodePort.ValueType != correspondingOutput.Type)) {
//				setStoryVariableNode.ApplyChanges();
//			}
//		}
//	}
//}