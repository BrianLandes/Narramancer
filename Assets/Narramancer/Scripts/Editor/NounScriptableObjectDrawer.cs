using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Narramancer {
	[CustomEditor(typeof(NounScriptableObject))]
	public class NounScriptableObjectDrawer : Editor {
		ReorderableList propertiesList;
		ReorderableList statsList;
		ReorderableList relationshipsList;
		ReorderableList variableList;

		bool openObjectPicker = false;
		const int PROPERTIES_PICKER = 1 << 1;
		const int STATS_PICKER = 1 << 2;

		public override void OnInspectorGUI() {

			serializedObject.Update();

			using (new EditorGUI.DisabledScope(true)) {
				var script = serializedObject.FindProperty("m_Script");
				EditorGUILayout.PropertyField(script, true);
			}

			var uid = serializedObject.FindProperty("uid");
			EditorGUILayout.PropertyField(uid, true);

			var displayName = serializedObject.FindProperty("displayName");
			EditorGUILayout.PropertyField(displayName, true);

			if (propertiesList == null) {
				var properties = serializedObject.FindProperty("properties");
				propertiesList = propertiesList != null ? propertiesList : new ReorderableList(serializedObject, properties, true, true, true, true);
				propertiesList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
					var element = properties.GetArrayElementAtIndex(index);
					EditorGUI.PropertyField(rect, element, true);
				};
				propertiesList.onAddCallback = list => {
					openObjectPicker = true;
					EditorGUIUtility.ShowObjectPicker<PropertyScriptableObject>(null, false, "", PROPERTIES_PICKER);
				};
				propertiesList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Starting Properties");
			}

			propertiesList.DoLayoutList();

			#region Accept DragAndDrop
			if (Event.current.type == EventType.DragUpdated) {
				DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
			}
			if (Event.current.type == EventType.DragPerform) {
				DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
				var lastRect = GUILayoutUtility.GetLastRect();
				if (lastRect.Contains(Event.current.mousePosition)) {

					if (DragAndDrop.objectReferences.Any(@object => @object is PropertyScriptableObject)) {
						DragAndDrop.AcceptDrag();

						var selectedObjects = DragAndDrop.objectReferences.Where(@object => @object is PropertyScriptableObject);

						var properties = serializedObject.FindProperty("properties");
						foreach (var selectedObject in selectedObjects) {
							var newPropertyAssignment = properties.CreateNewElement();
							var propertyAssignmentProperty = newPropertyAssignment.FindPropertyRelative(nameof(PropertyAssignment.property));
							propertyAssignmentProperty.objectReferenceValue = selectedObject;
						}
					}
				}

			}
			#endregion


			if (statsList == null) {
				var stats = serializedObject.FindProperty("stats");
				statsList = statsList != null ? statsList : new ReorderableList(serializedObject, stats, true, true, true, true);
				statsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
					var element = stats.GetArrayElementAtIndex(index);
					EditorGUI.PropertyField(rect, element, true);
				};
				statsList.onAddCallback = list => {
					openObjectPicker = true;
					EditorGUIUtility.ShowObjectPicker<StatScriptableObject>(null, false, "", STATS_PICKER);
				};
				statsList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Starting Stats");
			}

			statsList.DoLayoutList();

			#region Accept DragAndDrop
			if (Event.current.type == EventType.DragPerform) {
				DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
				var lastRect = GUILayoutUtility.GetLastRect();
				if (lastRect.Contains(Event.current.mousePosition)) {

					if (DragAndDrop.objectReferences.Any(@object => @object is StatScriptableObject)) {
						DragAndDrop.AcceptDrag();

						var selectedObjects = DragAndDrop.objectReferences.Where(@object => @object is StatScriptableObject);

						var stats = serializedObject.FindProperty("stats");
						foreach (var selectedObject in selectedObjects) {
							var newStatAssignment = stats.CreateNewElement();
							var statAssignmentProperty = newStatAssignment.FindPropertyRelative(nameof(StatAssignment.stat));
							statAssignmentProperty.objectReferenceValue = selectedObject;
						}
					}
				}

			}
			#endregion


			if (relationshipsList == null) {
				var relationships = serializedObject.FindProperty("relationships");
				relationshipsList = relationshipsList != null ? relationshipsList : new ReorderableList(serializedObject, relationships, true, true, true, true);
				relationshipsList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
					var element = relationships.GetArrayElementAtIndex(index);
					EditorGUI.PropertyField(rect, element, true);
				};
				relationshipsList.drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Starting Relationships");
			}

			relationshipsList.DoLayoutList();

			if (openObjectPicker && Event.current.commandName == "ObjectSelectorSelectionDone") {
				openObjectPicker = false;
				var selectedObject = EditorGUIUtility.GetObjectPickerObject();

				switch (EditorGUIUtility.GetObjectPickerControlID()) {
					case PROPERTIES_PICKER:
						var properties = serializedObject.FindProperty("properties");
						properties.arraySize++;
						var newProperty = properties.GetArrayElementAtIndex(properties.arraySize - 1);
						var propertyPropertyProperty = newProperty.FindPropertyRelative(nameof(PropertyAssignment.property));
						propertyPropertyProperty.objectReferenceValue = selectedObject;
						break;
					case STATS_PICKER:
						var stats = serializedObject.FindProperty("stats");
						stats.arraySize++;
						var newStat = stats.GetArrayElementAtIndex(stats.arraySize - 1);
						var statProperty = newStat.FindPropertyRelative(nameof(StatAssignment.stat));
						statProperty.objectReferenceValue = selectedObject;
						var valueProperty = newStat.FindPropertyRelative(nameof(StatAssignment.value));
						valueProperty.floatValue = 0f;
						break;
				}

			}

			#region Accept DragAndDrop
			if (Event.current.type == EventType.DragPerform) {
				DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
				var lastRect = GUILayoutUtility.GetLastRect();
				if (lastRect.Contains(Event.current.mousePosition)) {

					if (DragAndDrop.objectReferences.Any(@object => @object is RelationshipScriptableObject)) {
						DragAndDrop.AcceptDrag();

						var selectedObjects = DragAndDrop.objectReferences.Where(@object => @object is RelationshipScriptableObject);

						var relationships = serializedObject.FindProperty("relationships");
						foreach (var selectedObject in selectedObjects) {
							var newRelationshipAssignment = relationships.CreateNewElement();
							var relationshipAssignmentProperty = newRelationshipAssignment.FindPropertyRelative(nameof(RelationshipAssignment.relationship));
							relationshipAssignmentProperty.objectReferenceValue = selectedObject;
						}
					}
				}

			}
			#endregion


			var startingBlackboard = serializedObject.FindProperty("startingBlackboard");
			EditorGUILayout.PropertyField(startingBlackboard);

			var blackboardAssignments = serializedObject.FindProperty("blackboardAssignments");

			if (variableList == null) {
				variableList = new ReorderableList(serializedObject, blackboardAssignments, true, true, true, true);
				variableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
					var element = variableList.serializedProperty.GetArrayElementAtIndex(index);
					EditorGUI.PropertyField(rect, element, GUIContent.none);
				};
				variableList.headerHeight = EditorGUIUtility.singleLineHeight * 2f;
				variableList.drawHeaderCallback = (rect) => {
					var name = blackboardAssignments.propertyPath.Nicify();
					var headerRect = new Rect(rect.x, rect.y, rect.width, rect.height * 0.5f);
					EditorGUI.LabelField(headerRect, name);

					var style = new GUIStyle(GUI.skin.label);
					style.alignment = TextAnchor.LowerCenter;
					style.fontSize = 12;

					var typeRect = new Rect(rect.x, rect.y + headerRect.height, rect.width * 0.3f, headerRect.height);
					EditorGUI.LabelField(typeRect, "Type", style);

					var nameRect = new Rect(rect.x + rect.width * 0.3f, rect.y + headerRect.height, rect.width * 0.4f, headerRect.height);
					EditorGUI.LabelField(nameRect, "Name", style);

					var valueRect = new Rect(rect.x + rect.width * 0.7f, rect.y + headerRect.height, rect.width * 0.3f, headerRect.height);
					EditorGUI.LabelField(valueRect, "Starting Value", style);
				};

				variableList.onAddCallback = list => {
					EditorDrawerUtilities.ShowTypeSelectionPopup(type => {
						blackboardAssignments.InsertArrayElementAtIndex(blackboardAssignments.arraySize);
						var newElement = blackboardAssignments.GetArrayElementAtIndex(blackboardAssignments.arraySize - 1);
						var typeProperty = newElement.FindPropertyRelative(NarramancerPort.TypeFieldName);
						var typeTypeProperty = typeProperty.FindPropertyRelative(SerializableType.TypeFieldName);
						typeTypeProperty.stringValue = type.AssemblyQualifiedName;
						var nameProperty = newElement.FindPropertyRelative(NarramancerPort.NameFieldName);
						nameProperty.stringValue = type.Name.Uncapitalize();
						serializedObject.ApplyModifiedProperties();
					});

				};

			}

			variableList.DoLayoutList();



			serializedObject.ApplyModifiedProperties();
		}

	}
}