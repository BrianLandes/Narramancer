using Narramancer.SerializableDictionary;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XNode;
using XNodeEditor;

namespace Narramancer {

	[CustomNodeEditor(typeof(SwitchOnPrimitiveNode))]
	public class SwitchOnPrimitiveNodeEditor : ResizableNodeEditor {

		public override void OnBodyGUI() {

			serializedObject.Update();

			EditorGUILayout.BeginVertical();

			var inputType = serializedObject.FindProperty(SwitchOnPrimitiveNode.InputTypeFieldName);
			EditorGUILayout.PropertyField(inputType);

			var outputType = serializedObject.FindProperty(SwitchOnPrimitiveNode.OutputTypeFieldName);
			EditorGUILayout.PropertyField(outputType);

			var inputPort = target.GetDynamicInput(SwitchOnPrimitiveNode.INPUT_ELEMENT);
			if (inputPort != null) {
				NodeEditorGUILayout.PortField(inputPort, serializedObject);
			}

			var defaultValuePort = target.GetDynamicInput(SwitchOnPrimitiveNode.DEFAULT_OUTPUT_VALUE);
			if (defaultValuePort != null) {
				NodeEditorGUILayout.PortField(defaultValuePort, serializedObject);
			}

			var outputPort = target.GetDynamicOutput(SwitchOnPrimitiveNode.OUTPUT_ELEMENT);
			if (outputPort != null) {
				NodeEditorGUILayout.PortField(outputPort, serializedObject);
			}

			var pairings = serializedObject.FindProperty(SwitchOnPrimitiveNode.PairingsFieldName);

			pairings.isExpanded = EditorGUILayout.Foldout(pairings.isExpanded, "Pairings", true);
			if (pairings.isExpanded) {

				var keys = pairings.FindPropertyRelative(SerializableDictionaryPropertyDrawer.KeysFieldName);
				var values = pairings.FindPropertyRelative(SerializableDictionaryPropertyDrawer.ValuesFieldName);

				for (var ii = 0; ii < keys.arraySize; ii++) {
					GUILayout.BeginVertical("box");

					GUILayout.BeginHorizontal();

					var key = keys.GetArrayElementAtIndex(ii);
					EditorGUILayout.PropertyField(key, GUIContent.none);

					var element = values.GetArrayElementAtIndex(ii);
					EditorGUILayout.PropertyField(element, GUIContent.none);

					if (GUILayout.Button("X", GUILayout.Width(20))) {
						var node = target as SwitchOnPrimitiveNode;
						node.RemovePairing(ii);
						serializedObject.ApplyModifiedProperties();
						serializedObject.Update();
					}

					GUILayout.EndHorizontal();

					//var nodePortName = element.FindPropertyRelative(nameof(ChooseSpriteBasedOnFloatNode.Pairing.portName));
					//var nodePort = target.GetInputPort(nodePortName.stringValue);
					//NodeEditorGUILayout.PortField(GUIContent.none, nodePort, serializedObject);

					GUILayout.EndVertical();

				}

				if (GUILayout.Button("Add Pairing")) {
					var node = target as SwitchOnPrimitiveNode;
					node.AddNewPairing();
				}

			}

			EditorGUILayout.EndVertical();

			#region Accept DragAndDrop
			if (Event.current.type == EventType.DragUpdated) {
				DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
			}
			if (Event.current.type == EventType.DragPerform) {
				DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
				var lastRect = GUILayoutUtility.GetLastRect();
				if (lastRect.Contains(Event.current.mousePosition)) {

					var inputTypeTypeProperty = inputType.FindPropertyRelative(SerializableType.TypeFieldName);
					var typeString = inputTypeTypeProperty.stringValue;

					var selectedObjects = DragAndDrop.objectReferences.Where(@object => @object.GetType().AssemblyQualifiedName == typeString);

					if (selectedObjects.Any()) {
						DragAndDrop.AcceptDrag();

						foreach (var selectedObject in selectedObjects) {

							pairings = serializedObject.FindProperty(SwitchOnPrimitiveNode.PairingsFieldName);
							var keys = pairings.FindPropertyRelative(SerializableDictionaryPropertyDrawer.KeysFieldName);
							var ii = keys.arraySize;

							serializedObject.ApplyModifiedProperties();
							var node = target as SwitchOnPrimitiveNode;
							node.AddNewPairing();
							serializedObject.Update();

							pairings = serializedObject.FindProperty(SwitchOnPrimitiveNode.PairingsFieldName);
							keys = pairings.FindPropertyRelative(SerializableDictionaryPropertyDrawer.KeysFieldName);

							var newKey = keys.GetArrayElementAtIndex(ii);
							var objectValue = newKey.FindPropertyRelative(nameof(SerializablePrimitive.objectValue));
							objectValue.objectReferenceValue = selectedObject;

						}
					}
				}

			}
			#endregion

			serializedObject.ApplyModifiedProperties();

			DrawResizableButton();
		}
	}
}