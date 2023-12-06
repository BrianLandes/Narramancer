//using System;
//using System.Collections.Generic;
//using System.Linq;
//using UnityEditor;
//using UnityEngine;
//using XNodeEditor;

//namespace Narramancer {
//	[CustomNodeEditor(typeof(SetVariableNode))]
//	public class SetVariableNodeEditor : ChainedRunnableNodeEditor {

//		public override void OnBodyGUI() {

//			OnTopGUI();

//			var setVariableNode = target as SetVariableNode;

//			serializedObject.Update();


//			var variableId = serializedObject.FindProperty(SetVariableNode.VariableIdFieldName);
//			var nameProperty = serializedObject.FindProperty(SetVariableNode.VariableNameFieldName);
//			var keyProperty = serializedObject.FindProperty(SetVariableNode.VariableKeyFieldName);

//			var scopeTypeProperty = serializedObject.FindProperty(SetVariableNode.ScopeFieldName);
//			EditorGUI.BeginChangeCheck();
//			EditorGUILayout.PropertyField(scopeTypeProperty);
//			if (EditorGUI.EndChangeCheck()) {
//				variableId.stringValue = string.Empty;
//				nameProperty.stringValue = string.Empty;
//				keyProperty.stringValue = string.Empty;
//				serializedObject.ApplyModifiedProperties();
//				serializedObject.Update();
//			}

//			var variables = setVariableNode.GetScopeVariables();

//			if ( setVariableNode.IsSceneScopeAndCurrentSceneIsNotLoaded() ) {
//				var text = $"Associated with a variable in a different scene: {setVariableNode.Scene}";
//				EditorGUILayout.HelpBox(text, MessageType.Info);
//			}

//			if (variables == null) {
//				if (variableId.stringValue.IsNotNullOrEmpty()) {

//					var nodePort = setVariableNode.GetInputPort(SetVariableNode.PORT_NAME);
//					var originalColor = GUI.color;
//					if (nodePort != null) {
//						GUI.color = NodeEditorPreferences.GetTypeColor(nodePort.ValueType);
//					}

//					var text = setVariableNode.GetVariableLabel();

//					EditorGUILayout.LabelField(new GUIContent(text, text));

//					GUI.color = originalColor;

//					if (nodePort != null) {

//						bool IsTypeThatCanShowBackend(Type type) {
//							return typeof(bool).IsAssignableFrom(type) || typeof(string).IsAssignableFrom(type) || typeof(int).IsAssignableFrom(type)
//								|| typeof(float).IsAssignableFrom(type) || typeof(UnityEngine.Object).IsAssignableFrom(type);
//						}

//						if (IsTypeThatCanShowBackend(nodePort.ValueType) && !nodePort.IsConnected) {
//							NodeEditorGUILayout.PortField(GUIContent.none, nodePort, serializedObject);
//						}
//						else {
//							GUILayout.Space(-EditorGUIUtility.singleLineHeight);
//							NodeEditorGUILayout.PortField(GUIContent.none, nodePort, serializedObject);
//						}

//					}
//				}
//			}
//			else {
//				if (variableId.stringValue.IsNullOrEmpty() && variables.Any()) {
//					var firstVariable = variables.First();
//					variableId.stringValue = firstVariable.Id;
//					nameProperty.stringValue = firstVariable.Name;
//					keyProperty.stringValue = firstVariable.VariableKey;
//					serializedObject.ApplyModifiedProperties();

//					setVariableNode.ApplyChanges();
//				}

//				var buttonText = string.Empty;

//				if (nameProperty.stringValue.IsNotNullOrEmpty()) {

//					buttonText = nameProperty.stringValue;
//				}
//				else {
//					buttonText = "(None)";
//				}

//				var originalColor = GUI.color;

//				var correspondingOutput = variables.FirstOrDefault(output => output.Id.Equals(variableId.stringValue));
//				var nodePort = setVariableNode.GetInputPort(SetVariableNode.PORT_NAME);

//				if (nodePort != null) {
//					GUI.color = NodeEditorPreferences.GetTypeColor(nodePort.ValueType);
//				}

//				if (EditorGUILayout.DropdownButton(new GUIContent(buttonText, buttonText), FocusType.Passive)) {
//					GenericMenu context = new GenericMenu();

//					foreach (var variable in variables) {
//						context.AddItem(new GUIContent(variable.ToString()), variable.Id == variableId.stringValue, () => {
//							serializedObject.Update();
//							correspondingOutput = variable;
//							variableId.stringValue = correspondingOutput.Id;
//							nameProperty.stringValue = correspondingOutput.Name;
//							keyProperty.stringValue = correspondingOutput.VariableKey;
//							serializedObject.ApplyModifiedProperties();

//							setVariableNode.ApplyChanges();
//						});
//					}

//					if (context.GetItemCount() == 0) {
//						context.AddDisabledItem(new GUIContent("(No valid values)"));
//					}

//					Matrix4x4 originalMatrix = GUI.matrix;
//					GUI.matrix = Matrix4x4.identity;
//					context.ShowAsContext();
//					GUI.matrix = originalMatrix;

//				}

//				GUI.color = originalColor;

//				if (nodePort != null) {

//					bool IsTypeThatCanShowBackend(Type type) {
//						return typeof(bool).IsAssignableFrom(type) || typeof(string).IsAssignableFrom(type) || typeof(int).IsAssignableFrom(type)
//							|| typeof(float).IsAssignableFrom(type) || typeof(UnityEngine.Object).IsAssignableFrom(type);
//					}

//					if (IsTypeThatCanShowBackend(nodePort.ValueType) && !nodePort.IsConnected) {
//						NodeEditorGUILayout.PortField(GUIContent.none, nodePort, serializedObject);
//					}
//					else {
//						GUILayout.Space(-EditorGUIUtility.singleLineHeight);
//						NodeEditorGUILayout.PortField(GUIContent.none, nodePort, serializedObject);
//					}

//				}

//				serializedObject.ApplyModifiedProperties();

//				if (correspondingOutput != null && (nodePort == null || nodePort.ValueType != correspondingOutput.Type)) {
//					setVariableNode.ApplyChanges();
//				}
//			}

			
//		}
//	}
//}