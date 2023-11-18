using Narramancer.SerializableDictionary;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Narramancer {
	[CustomEditor(typeof(NounScriptableObject))]
	public class NounScriptableObjectDrawer : Editor {
		ReorderableList propertiesList;
		ReorderableList statsList;
		ReorderableList relationshipsList;

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

			var nounType = serializedObject.FindProperty("nounType");
			EditorGUILayout.PropertyField(nounType, true);

			if (((NounType)nounType.enumValueIndex) == NounType.Character) {
				var pronouns = serializedObject.FindProperty("pronouns");
				EditorGUILayout.PropertyField(pronouns, true);
			}

			if (propertiesList == null) {
				var properties = serializedObject.FindProperty("properties");
				propertiesList = propertiesList!=null ? propertiesList : new ReorderableList(serializedObject, properties, true, true, true, true);
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

			if (statsList == null) {
				var stats = serializedObject.FindProperty("stats");
				statsList = statsList!=null ? statsList : new ReorderableList(serializedObject, stats, true, true, true, true);
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

			if (relationshipsList == null) {
				var relationships = serializedObject.FindProperty("relationships");
				relationshipsList = relationshipsList!=null ? relationshipsList : new ReorderableList(serializedObject, relationships, true, true, true, true);
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

			var startingBlackboard = serializedObject.FindProperty("startingBlackboard");
			EditorGUILayout.PropertyField(startingBlackboard);

			serializedObject.ApplyModifiedProperties();
		}

	}
}