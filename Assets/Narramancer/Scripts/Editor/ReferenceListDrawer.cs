using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Narramancer {

	[CustomPropertyDrawer(typeof(ReferenceList))]
	public class ReferenceListDrawer : PropertyDrawer {

		bool referencesUpdated = false;
		bool firstTimeOpened = true;

		const float updateWidth = 40f;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

			var objects = property.FindPropertyRelative(nameof(ReferenceList.objects));

			if (firstTimeOpened) {
				objects.isExpanded = false;
				firstTimeOpened = false;
			}

			var headerRect = new Rect(position.x, position.y, position.width - updateWidth, EditorGUIUtility.singleLineHeight);
			objects.isExpanded = EditorGUI.Foldout(headerRect, objects.isExpanded, "Asset References", true);

			if (objects.isExpanded) {
				var buttonRect = new Rect(position.x + headerRect.width, position.y, updateWidth, EditorGUIUtility.singleLineHeight);

				if (GUI.Button(buttonRect, EditorGUIUtility.IconContent("d_Refresh@2x")) || !referencesUpdated) {
					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();

					var referenceList = property.GetTargetObject<ReferenceList>();

					referenceList.objects.Clear();

					var targetObject = property.serializedObject.targetObject;
					var targetType = targetObject.GetType();
					var scriptableObjectTypes = AssemblyUtilities.GetAllTypes<ScriptableObject>();
					var typesThatUseType = scriptableObjectTypes.Where(type => type.HasFieldWithType(targetType)).ToArray();

					var instances = PseudoEditorUtilities.GetAllInstances(typesThatUseType);

					var instancesThatReferenceObject = instances.Where(instance => instance.FieldHasValue(targetType, targetObject)).ToArray();

					referenceList.objects.AddRange(instancesThatReferenceObject);

					referencesUpdated = true;
				}

				EditorGUI.BeginDisabledGroup(true);

				var elementRect = new Rect(position.x + 20, position.y + headerRect.height + EditorGUIUtility.standardVerticalSpacing, position.width - 20, EditorGUIUtility.singleLineHeight);

				for (int ii = 0; ii < objects.arraySize; ii++) {
					var element = objects.GetArrayElementAtIndex(ii);

					EditorGUI.PropertyField(elementRect, element, GUIContent.none);

					elementRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
				}

				EditorGUI.EndDisabledGroup();

				if (objects.arraySize == 0) {
					EditorGUI.LabelField(elementRect, "No references found");
				}
			}
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
			var objects = property.FindPropertyRelative(nameof(ReferenceList.objects));
			if (objects.isExpanded) {
				var itemHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
				return (objects.arraySize + 2) * itemHeight;
			}
			return EditorGUIUtility.singleLineHeight;
		}
	}
}