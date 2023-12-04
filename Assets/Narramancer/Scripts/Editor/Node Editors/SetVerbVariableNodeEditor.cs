//using System;
//using System.Linq;
//using UnityEditor;
//using UnityEngine;
//using XNodeEditor;

//namespace Narramancer {
//	[CustomNodeEditor(typeof(SetVerbVariableNode))]
//	public class SetVerbVariableNodeEditor : ChainedRunnableNodeEditor {

//		public override void OnBodyGUI() {
//			var setGraphVariableNode = target as SetVerbVariableNode;
//			var graph = setGraphVariableNode.graph as VerbGraph;

//			if (graph == null) {
//				return;
//			}

//			serializedObject.Update();

//			OnTopGUI();

//			var outputId = serializedObject.FindProperty("outputId");
//			if (outputId.stringValue.IsNullOrEmpty() && graph.Outputs.Any()) {
//				outputId.stringValue = graph.Outputs.First().Id;
//				serializedObject.ApplyModifiedProperties();

//				setGraphVariableNode.ApplyChanges();
//			}

//			var buttonText = string.Empty;

//			var correspondingOutput = graph.Outputs.FirstOrDefault(output => output.Id.Equals(outputId.stringValue));
//			if (correspondingOutput != null) {

//				buttonText = correspondingOutput.ToString();
//			}
//			else {
//				buttonText = "(None)";
//			}

//			var originalColor = GUI.color;

//			var nodePort = correspondingOutput != null ? setGraphVariableNode.GetInputPort(SetVerbVariableNode.PORT_NAME) : null;

//			if (nodePort != null) {
//				GUI.color = NodeEditorPreferences.GetTypeColor(nodePort.ValueType);
//			}

//			if (EditorGUILayout.DropdownButton(new GUIContent(buttonText, buttonText), FocusType.Passive)) {
//				GenericMenu context = new GenericMenu();

//				foreach (var output in graph.Outputs) {
//					context.AddItem(new GUIContent(output.ToString()), output.Id == outputId.stringValue, () => {
//						serializedObject.Update();
//						correspondingOutput = output;
//						outputId.stringValue = correspondingOutput.Id;
//						serializedObject.ApplyModifiedProperties();

//						setGraphVariableNode.ApplyChanges();
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
//				setGraphVariableNode.ApplyChanges();
//			}
//		}
//	}
//}