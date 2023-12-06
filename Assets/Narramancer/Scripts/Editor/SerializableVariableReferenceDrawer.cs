using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XNode;
using XNodeEditor;

namespace Narramancer {

	[CustomPropertyDrawer(typeof(SerializableVariableReference))]
	public class SerializableVariableReferenceDrawer : PropertyDrawer {

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

			EditorGUI.BeginProperty(position, label, property);

			var variableReference = property.GetTargetObject<SerializableVariableReference>();

			property.serializedObject.Update();


			var variableId = property.FindPropertyRelative(SerializableVariableReference.VariableIdFieldName);
			var nameProperty = property.FindPropertyRelative(SerializableVariableReference.VariableNameFieldName);
			var keyProperty = property.FindPropertyRelative(SerializableVariableReference.VariableKeyFieldName);


			var scopeTypeProperty = property.FindPropertyRelative(SerializableVariableReference.ScopeFieldName);
			EditorGUI.BeginChangeCheck();
			var scopeRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
			EditorGUI.PropertyField(scopeRect, scopeTypeProperty);
			if (EditorGUI.EndChangeCheck()) {
				variableId.stringValue = string.Empty;
				nameProperty.stringValue = string.Empty;
				keyProperty.stringValue = string.Empty;
				property.serializedObject.ApplyModifiedProperties();
				property.serializedObject.Update();
			}

			var node = property.serializedObject.targetObject as Node;
			var verbGraph = node?.graph as VerbGraph;

			var variables = variableReference.GetScopeVariables(verbGraph);

			//if (variableReference.IsSceneScopeAndCurrentSceneIsNotLoaded()) {
			//	var text = $"Associated with a variable in a different scene: {variableReference.Scene}";
			//	EditorGUILayout.HelpBox(text, MessageType.Info);
			//}

			if (variables == null) {
				if (variableId.stringValue.IsNotNullOrEmpty()) {

					var nodePort = node?.GetInputPort(SerializableVariableReference.PORT_NAME);
					var originalColor = GUI.color;
					if (nodePort != null) {
						GUI.color = NodeEditorPreferences.GetTypeColor(nodePort.ValueType);
					}

					var text = variableReference.GetVariableLabel(verbGraph);

					EditorGUILayout.LabelField(new GUIContent(text, text));

					GUI.color = originalColor;

					if (nodePort != null) {

						bool IsTypeThatCanShowBackend(Type type) {
							return typeof(bool).IsAssignableFrom(type) || typeof(string).IsAssignableFrom(type) || typeof(int).IsAssignableFrom(type)
								|| typeof(float).IsAssignableFrom(type) || typeof(UnityEngine.Object).IsAssignableFrom(type);
						}

						if (IsTypeThatCanShowBackend(nodePort.ValueType) && !nodePort.IsConnected) {
							NodeEditorGUILayout.PortField(GUIContent.none, nodePort, property.serializedObject);
						}
						else {
							GUILayout.Space(-EditorGUIUtility.singleLineHeight);
							NodeEditorGUILayout.PortField(GUIContent.none, nodePort, property.serializedObject);
						}

					}
				}
			}
			else {
				if (variableId.stringValue.IsNullOrEmpty() && variables.Any()) {
					var firstVariable = variables.First();
					variableId.stringValue = firstVariable.Id;
					nameProperty.stringValue = firstVariable.Name;
					keyProperty.stringValue = firstVariable.VariableKey;
					property.serializedObject.ApplyModifiedProperties();

					node?.UpdatePorts();
					variableReference.UpdateScene();
				}

				var buttonText = string.Empty;

				if (nameProperty.stringValue.IsNotNullOrEmpty()) {

					buttonText = nameProperty.stringValue;
				}
				else {
					buttonText = "(None)";
				}

				var originalColor = GUI.color;

				var correspondingOutput = variables.FirstOrDefault(output => output.Id.Equals(variableId.stringValue));
				var nodePort = node?.GetPort(SerializableVariableReference.PORT_NAME);

				if (nodePort != null) {
					GUI.color = NodeEditorPreferences.GetTypeColor(nodePort.ValueType);
				}

				var dropdownRect = new Rect(position.x, position.y + scopeRect.height + EditorGUIUtility.standardVerticalSpacing, position.width, EditorGUIUtility.singleLineHeight);

				if (EditorGUI.DropdownButton(dropdownRect, new GUIContent(buttonText, buttonText), FocusType.Passive)) {
					GenericMenu context = new GenericMenu();

					foreach (var variable in variables) {
						context.AddItem(new GUIContent(variable.ToString()), variable.Id == variableId.stringValue, () => {
							property.serializedObject.Update();
							correspondingOutput = variable;
							variableId.stringValue = correspondingOutput.Id;
							nameProperty.stringValue = correspondingOutput.Name;
							keyProperty.stringValue = correspondingOutput.VariableKey;
							property.serializedObject.ApplyModifiedProperties();

							node?.UpdatePorts();
							variableReference.UpdateScene();
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

				//if (nodePort != null) {

				//	bool IsTypeThatCanShowBackend(Type type) {
				//		return typeof(bool).IsAssignableFrom(type) || typeof(string).IsAssignableFrom(type) || typeof(int).IsAssignableFrom(type)
				//			|| typeof(float).IsAssignableFrom(type) || typeof(UnityEngine.Object).IsAssignableFrom(type);
				//	}

				//	if (IsTypeThatCanShowBackend(nodePort.ValueType) && !nodePort.IsConnected) {
				//		NodeEditorGUILayout.PortField(GUIContent.none, nodePort, property.serializedObject);
				//	}
				//	else {
				//		GUILayout.Space(-EditorGUIUtility.singleLineHeight);
				//		NodeEditorGUILayout.PortField(GUIContent.none, nodePort, property.serializedObject);
				//	}

				//}

				property.serializedObject.ApplyModifiedProperties();

				if (correspondingOutput != null && (nodePort == null || nodePort.ValueType != correspondingOutput.Type)) {
					node?.UpdatePorts();
					variableReference.UpdateScene();
				}
			}

			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {

			return EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;
		}
	}
}