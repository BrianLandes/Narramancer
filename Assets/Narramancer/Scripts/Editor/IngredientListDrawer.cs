using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Narramancer {
	[CustomPropertyDrawer(typeof(IngredientList<>))]
	public class IngredientListDrawer : PropertyDrawer {
		ReorderableList list;
		List<Editor> elementEditors = new List<Editor>();
		SerializedProperty property;
		Type ingredientInnerType;

		Color elementColor = new Color(0.2f, 0.2f, 0.2f);

		private ReorderableList GetList(SerializedProperty property) {
			if (list == null) {
				var values = property.FindPropertyRelative("values");
				list = new ReorderableList(property.serializedObject, values, true, true, true, true);
				list.elementHeightCallback = GetListItemHeight;
				list.drawElementCallback = DrawListItem;
				list.onAddCallback = OnAddCallback;
				list.onRemoveCallback = EditorDrawerUtilities.OnReorderableListRemoveCallbackRemoveChildAsset;
				list.drawHeaderCallback = DrawHeaderCallback;
			}
			return list;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			this.property = property;
			ingredientInnerType = ingredientInnerType!=null ? ingredientInnerType: GetListInnerType(property);

			var list = GetList(property);

			property.serializedObject.Update();

			EditorGUI.BeginProperty(position, label, property);

			list.DoList(position);

			EditorGUI.EndProperty();
			if (Event.current.type == EventType.Layout) {
				EditorGUILayout.Space(position.height);
			}

			property.serializedObject.ApplyModifiedProperties();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
			var list = GetList(property);
			if (list != null) {

				return list.GetHeight();
			}
			return base.GetPropertyHeight(property, label);
		}

		private Type GetListInnerType(SerializedProperty property) {
			var parentType = property.serializedObject.targetObject.GetType();
			var fieldInfo = parentType.GetField(property.propertyPath, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			var fieldType = fieldInfo.FieldType;
			var innerType = fieldType.GetGenericArguments()[0];
			return innerType;
		}

		private string GetKeywordFromIngredientType() {
			string typeName = ingredientInnerType.Name;

			typeName = typeName.Remove("Abstract");
			typeName = typeName.Remove("Ingredient");
			typeName = typeName.Remove("Base");

			return typeName;
		}

		private void DrawHeaderCallback(Rect rect) {
			var name = property.propertyPath.Nicify();
			EditorGUI.LabelField(rect, name);
		}

		private void OnAddCallback(ReorderableList list) {
			GenericMenu context = new GenericMenu();

			var types = AssemblyUtilities.GetAllNonObsoleteTypes(ingredientInnerType);

			foreach (var type in types) {
				string className = ObjectNames.NicifyVariableName(type.Name);

				className = className.Remove("Ingredient");

				className = className.Remove(GetKeywordFromIngredientType());

				string label = "Add " + className;

				context.AddItem(new GUIContent(label), false, () => {

					var newIngredient = ScriptableObject.CreateInstance(type);
					newIngredient.name = className;

					var path = AssetDatabase.GetAssetPath(list.serializedProperty.serializedObject.targetObject);
					var parent = AssetDatabase.LoadMainAssetAtPath(path);
					if (parent != null) {
						AssetDatabase.AddObjectToAsset(newIngredient, (ScriptableObject)parent);
					}

					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();

					list.serializedProperty.serializedObject.Update();

					list.serializedProperty.InsertArrayElementAtIndex(list.serializedProperty.arraySize);
					var newElement = list.serializedProperty.GetArrayElementAtIndex(list.serializedProperty.arraySize - 1);
					newElement.objectReferenceValue = newIngredient;

					list.serializedProperty.serializedObject.ApplyModifiedProperties();
				});
			}

			// Add Empty
			{

				context.AddItem(new GUIContent("Add Empty Entry"), false, () => {
					list.serializedProperty.serializedObject.Update();

					list.serializedProperty.InsertArrayElementAtIndex(list.serializedProperty.arraySize);
					var newElement = list.serializedProperty.GetArrayElementAtIndex(list.serializedProperty.arraySize - 1);
					newElement.objectReferenceValue = null;

					list.serializedProperty.serializedObject.ApplyModifiedProperties();
				});
			}

			context.ShowAsContext();
		}

		private float GetListItemHeight(int index) {
			var element = list.serializedProperty.GetArrayElementAtIndex(index);
			if (element.isExpanded) {
				var height = GetElementHeight(element, index);
				return EditorGUIUtility.singleLineHeight + height;
			}
			else {
				return EditorGUIUtility.singleLineHeight;
			}
		}

		private Editor GetElementEditor(SerializedProperty element, int index) {
			Editor elementEditor = null;
			if (index >= elementEditors.Count) {
				elementEditor = Editor.CreateEditor(element.objectReferenceValue);
				elementEditors.Add(elementEditor);
			}
			else {
				elementEditor = elementEditors[index];
				Editor.CreateCachedEditor(element.objectReferenceValue, null, ref elementEditor);
			}
			return elementEditor;
		}

		private float GetElementHeight(SerializedProperty element, int index) {
			if (element.objectReferenceValue == null) {
				return 0f;
			}
			var elementEditor = GetElementEditor(element, index);
			var height = 10f;
			SerializedProperty iterator = elementEditor.serializedObject.GetIterator();
			bool enterChildren = true;
			while (iterator.NextVisible(enterChildren)) {
				height += EditorGUI.GetPropertyHeight(iterator);
				height += EditorGUIUtility.standardVerticalSpacing;

				enterChildren = false;
			}
			return height;
		}

		private void DrawListItem(Rect rect, int index, bool isActive, bool isFocused) {
			var element = list.serializedProperty.GetArrayElementAtIndex(index);

			if (element.objectReferenceValue != null) {
				var foldoutRect = new Rect(rect.x + 12, rect.y, 8, EditorGUIUtility.singleLineHeight);
				element.isExpanded = EditorGUI.Foldout(foldoutRect, element.isExpanded, GUIContent.none);
			}

			EditorGUI.BeginDisabledGroup(element.objectReferenceValue != null);
			var headerRect = new Rect(rect.x + 20, rect.y, rect.width - 20, EditorGUIUtility.singleLineHeight);
			EditorGUI.ObjectField(headerRect, element, GUIContent.none);
			EditorGUI.EndDisabledGroup();

			if (element.isExpanded && element.objectReferenceValue != null) {
				var height = GetElementHeight(element, index);
				var backgroundRect = new Rect(rect.x + 5, rect.y + EditorGUIUtility.singleLineHeight + 2,
					rect.width - 3, height - 3);

				EditorGUI.DrawRect(backgroundRect, elementColor);

				var elementPropertyRect = new Rect(rect.x + 10, rect.y + EditorGUIUtility.singleLineHeight + 10,
					rect.width - 10, height);

				var elementEditor = GetElementEditor(element, index);

				EditorGUI.BeginChangeCheck();
				elementEditor.serializedObject.UpdateIfRequiredOrScript();
				SerializedProperty iterator = elementEditor.serializedObject.GetIterator();
				bool enterChildren = true;
				while (iterator.NextVisible(enterChildren)) {
					elementPropertyRect.height = EditorGUI.GetPropertyHeight(iterator);

					using (new EditorGUI.DisabledScope("m_Script" == iterator.propertyPath)) {
						EditorGUI.PropertyField(elementPropertyRect, iterator, true);

					}
					elementPropertyRect.y += elementPropertyRect.height + EditorGUIUtility.standardVerticalSpacing;

					enterChildren = false;
				}

				elementEditor.serializedObject.ApplyModifiedProperties();
				EditorGUI.EndChangeCheck();
			}
		}

	}
}