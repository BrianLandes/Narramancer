using System.Linq;
using UnityEditor;
using UnityEngine;
using XNode;
using XNodeEditor;

namespace Narramancer {

	[CustomNodeEditor(typeof(OfferObjectsAsChoicesNode))]
	public class OfferObjectsAsChoicesNodeEditor : RunnableNodeEditor {

		public override void OnBodyGUI() {

			var thisNodePort = target.GetInputPort(RunnableNode.ThisNodeField);
			NodeEditorGUILayout.PortField(thisNodePort, serializedObject);

			var typeProperty = serializedObject.FindProperty(OfferObjectsAsChoicesNode.TypeFieldName);
			EditorGUILayout.PropertyField(typeProperty);


			var inputElementsPort= target.GetDynamicInput(OfferObjectsAsChoicesNode.INPUT_ELEMENTS);
			if (inputElementsPort != null) {
				NodeEditorGUILayout.PortField(inputElementsPort, serializedObject);
			}

			var inputListPort = target.GetDynamicInput(OfferObjectsAsChoicesNode.INPUT_LIST);
			if (inputListPort != null) {
				NodeEditorGUILayout.PortField(inputListPort, serializedObject);
			}

			var runWhenObjectSelectedPort = target.GetOutputPort(OfferObjectsAsChoicesNode.RunWhenObjectSelectedFieldName);
			NodeEditorGUILayout.PortField(runWhenObjectSelectedPort, serializedObject);

			var selectedElementPort = target.GetDynamicOutput(OfferObjectsAsChoicesNode.SELECTED_ELEMENT);
			if (selectedElementPort != null) {
				NodeEditorGUILayout.PortField(selectedElementPort, serializedObject);
			}

			SerializedProperty addOptionForBackProperty = null;
			using (EditorDrawerUtilities.WideLabel()) {
				addOptionForBackProperty = serializedObject.FindProperty(OfferObjectsAsChoicesNode.AddOptionForBackFieldName);
				EditorGUILayout.PropertyField(addOptionForBackProperty);
			}

			if (addOptionForBackProperty.boolValue) {
				var runWhenBackSelectedPort = target.GetOutputPort(OfferObjectsAsChoicesNode.RunWhenBackSelectedFieldName);
				NodeEditorGUILayout.PortField(runWhenBackSelectedPort, serializedObject);
			}

			SerializedProperty useValueVerbForDisplaynameProperty = null;
			using (EditorDrawerUtilities.WideLabel()) {
				useValueVerbForDisplaynameProperty = serializedObject.FindProperty(OfferObjectsAsChoicesNode.UseValueVerbForDisplaynameFieldName);
				EditorGUILayout.PropertyField(useValueVerbForDisplaynameProperty);
			}

			if (useValueVerbForDisplaynameProperty.boolValue) {
				var displayNamePredicatePort = serializedObject.FindProperty(OfferObjectsAsChoicesNode.DisplayNamePredicateFieldName);
				EditorGUILayout.PropertyField(displayNamePredicatePort);
			}


			SerializedProperty useValueVerbForEnabledProperty = null;
			using (EditorDrawerUtilities.WideLabel()) {
				useValueVerbForEnabledProperty = serializedObject.FindProperty(OfferObjectsAsChoicesNode.UseValueVerbForEnabledFieldName);
				EditorGUILayout.PropertyField(useValueVerbForEnabledProperty);
			}

			if (useValueVerbForEnabledProperty.boolValue) {
				var enabledPredicatePort = serializedObject.FindProperty(OfferObjectsAsChoicesNode.EnabledPredicateFieldName);
				EditorGUILayout.PropertyField(enabledPredicatePort);

				using (EditorDrawerUtilities.WideLabel()) {
					var showIfDisabledProperty = serializedObject.FindProperty(OfferObjectsAsChoicesNode.ShowIfDisabledFieldName);
					EditorGUILayout.PropertyField(showIfDisabledProperty);
				}
			}

		}


		public override bool HasCustomDroppedPortLogic() {
			var typeProperty = serializedObject.FindProperty(OfferObjectsAsChoicesNode.TypeFieldName);
			var typeTypeProperty = typeProperty.FindPropertyRelative(SerializableType.TypeFieldName);
			return typeTypeProperty.stringValue.IsNullOrEmpty();
		}

		public override void PerformCustomDroppedPortLogic(Node hoveredNode, NodePort draggedOutput) {
			var typeProperty = serializedObject.FindProperty(OfferObjectsAsChoicesNode.TypeFieldName);
			var typeTypeProperty = typeProperty.FindPropertyRelative(SerializableType.TypeFieldName);
			if( typeTypeProperty.stringValue.IsNotNullOrEmpty()) {
				return;
			}

			var nodePortType = draggedOutput.ValueType;
			var elementType = nodePortType;
			if (AssemblyUtilities.IsListType(nodePortType)) {
				elementType = AssemblyUtilities.GetListInnerType(nodePortType);
			}
			if (nodePortType != null) {
				var offerNode = target as OfferObjectsAsChoicesNode;
				offerNode.ElementType.Type = elementType;

				if (AssemblyUtilities.IsListType(nodePortType)) {
					var inputListPort = target.GetDynamicInput(OfferObjectsAsChoicesNode.INPUT_LIST);
					inputListPort.Connect(draggedOutput);
				}
				else {
					var inputElementsPort = target.GetDynamicInput(OfferObjectsAsChoicesNode.INPUT_ELEMENTS);
					inputElementsPort.Connect(draggedOutput);
				}
			}
			
			

		}
	}
}