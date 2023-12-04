using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Narramancer {
	[CustomNodeEditor(typeof(SetVariableNode))]
	public class SetVariableNodeEditor : ChainedRunnableNodeEditor {

		public override void OnBodyGUI() {

			OnTopGUI();

			var setVariableNode = target as SetVariableNode;

			serializedObject.Update();

			var variableId = serializedObject.FindProperty(SetVariableNode.VariableIdFieldName);

			var scopeTypeProperty = serializedObject.FindProperty(SetVariableNode.ScopeFieldName);
			EditorGUI.BeginChangeCheck();
			EditorGUILayout.PropertyField(scopeTypeProperty);
			if (EditorGUI.EndChangeCheck()) {
				variableId.stringValue = string.Empty;
				serializedObject.ApplyModifiedProperties();
				serializedObject.Update();
			}

			var variables = setVariableNode.GetScopeVariables();

			if (variableId.stringValue.IsNullOrEmpty() && variables.Any()) {
				variableId.stringValue = variables.First().Id;
				serializedObject.ApplyModifiedProperties();

				setVariableNode.ApplyChanges();
			}

			var buttonText = string.Empty;

			var correspondingOutput = variables.FirstOrDefault(output => output.Id.Equals(variableId.stringValue));
			if (correspondingOutput != null) {

				buttonText = correspondingOutput.ToString();
			}
			else {
				buttonText = "(None)";
			}

			var originalColor = GUI.color;

			var nodePort = correspondingOutput != null ? setVariableNode.GetInputPort(SetVariableNode.PORT_NAME) : null;

			if (nodePort != null) {
				GUI.color = NodeEditorPreferences.GetTypeColor(nodePort.ValueType);
			}

			if (EditorGUILayout.DropdownButton(new GUIContent(buttonText, buttonText), FocusType.Passive)) {
				GenericMenu context = new GenericMenu();

				foreach (var variable in variables) {
					context.AddItem(new GUIContent(variable.ToString()), variable.Id == variableId.stringValue, () => {
						serializedObject.Update();
						correspondingOutput = variable;
						variableId.stringValue = correspondingOutput.Id;
						serializedObject.ApplyModifiedProperties();

						setVariableNode.ApplyChanges();
					});
				}

				if (context.GetItemCount() == 0) {
					context.AddDisabledItem(new GUIContent("(No valid values)"));
				}

				Matrix4x4 originalMatrix = GUI.matrix;
				GUI.matrix = Matrix4x4.identity;
				context.ShowAsContext();
				GUI.matrix = originalMatrix;

			}

			GUI.color = originalColor;

			if (nodePort != null) {

				bool IsTypeThatCanShowBackend(Type type) {
					return typeof(bool).IsAssignableFrom(type) || typeof(string).IsAssignableFrom(type) || typeof(int).IsAssignableFrom(type)
						|| typeof(float).IsAssignableFrom(type) || typeof(UnityEngine.Object).IsAssignableFrom(type);
				}

				if (IsTypeThatCanShowBackend(nodePort.ValueType) && !nodePort.IsConnected) {
					NodeEditorGUILayout.PortField(GUIContent.none, nodePort, serializedObject);
				}
				else {
					GUILayout.Space(-EditorGUIUtility.singleLineHeight);
					NodeEditorGUILayout.PortField(GUIContent.none, nodePort, serializedObject);
				}

			}

			serializedObject.ApplyModifiedProperties();

			if (correspondingOutput != null && (nodePort == null || nodePort.ValueType != correspondingOutput.Type)) {
				setVariableNode.ApplyChanges();
			}
		}
	}
}