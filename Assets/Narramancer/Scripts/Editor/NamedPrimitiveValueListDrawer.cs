
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Narramancer {
	[CustomPropertyDrawer(typeof(NamedPrimitiveValueList))]
	public class NamedPrimitiveValueListDrawer : PropertyDrawer {

		ReorderableList list;

		string search;

		private ReorderableList GetList(SerializedProperty property) {
			if (list == null) {
				var listProperty = property.FindPropertyRelative(nameof(NamedPrimitiveValueList.list));
				list = new ReorderableList(property.serializedObject, listProperty, true, false, true, true);

				list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
					var element = listProperty.GetArrayElementAtIndex(index);
					EditorGUI.PropertyField(rect, element);
				};

				list.onAddCallback = list => {
					EditorDrawerUtilities.ShowTypeSelectionPopup(type => {
						var newElement = listProperty.CreateNewElement();
						var nameProperty = newElement.FindPropertyRelative(nameof(NamedPrimitiveValue.name));
						nameProperty.stringValue = type.Name.Nicify();
						var valueProperty = newElement.FindPropertyRelative(nameof(NamedPrimitiveValue.value));
						var valueTypeProperty = valueProperty.FindPropertyRelative(nameof(SerializablePrimitive.type));
						valueTypeProperty.stringValue = SerializablePrimitive.TypeToString(type);

						property.serializedObject.ApplyModifiedProperties();
					},
					typeFilter: SerializablePrimitive.IsSupportedType);
				};

			}
			return list;
		}



		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

			property.serializedObject.Update();

			EditorGUI.BeginProperty(position, label, property);

			var listProperty = property.FindPropertyRelative(nameof(NamedPrimitiveValueList.list));

			#region Search Bar
			var searchIconRect = new Rect(position.x, position.y, 30, EditorGUIUtility.singleLineHeight);
			var searchCloseRect = new Rect(position.x + position.width - 30, position.y, 30, EditorGUIUtility.singleLineHeight);
			var searchTextRect = new Rect(position.x + searchIconRect.width, position.y, position.width - searchIconRect.width - searchCloseRect.width, EditorGUIUtility.singleLineHeight);
			EditorGUI.LabelField(searchIconRect, EditorGUIUtility.IconContent("d_Search Icon"));
			search = EditorGUI.TextField(searchTextRect, search);
			if (GUI.Button(searchCloseRect, EditorGUIUtility.IconContent("winbtn_win_close"))) {
				EditorGUI.FocusTextInControl("");
				search = string.Empty;
			}
			#endregion

			if (search.IsNotNullOrEmpty()) {

				var searchLower = search.ToLower();
				var searchTerms = searchLower.Split(' ');

				bool ContainsAnySearchTerms(SerializedProperty value) {
					var nameProperty = value.FindPropertyRelative(nameof(NamedPrimitiveValue.name));
					var fullName = nameProperty.stringValue.ToLower();
					if (searchTerms.All(term => fullName.Contains(term))) {
						return true;
					}
					return false;
				}

				var objectRect = new Rect(position.x, searchTextRect.y + searchTextRect.height, position.width, EditorGUIUtility.singleLineHeight);

				for (var ii = 0; ii < listProperty.arraySize; ii++) {
					var element = listProperty.GetArrayElementAtIndex(ii);

					if (!ContainsAnySearchTerms(element)) {
						continue;
					}
					EditorGUI.PropertyField(objectRect, element);
					objectRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

				}
			}
			else {
				var listRect = new Rect(position.x, position.y + searchTextRect.height + EditorGUIUtility.standardVerticalSpacing,
					position.width, position.height - searchTextRect.height - EditorGUIUtility.standardVerticalSpacing);
				var list = GetList(property);
				list.DoList(listRect);
			}

			#region Accept DragAndDrop
			if (Event.current.type == EventType.DragUpdated) {
				DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
			}
			if (Event.current.type == EventType.DragPerform) {
				DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
				var lastRect = GUILayoutUtility.GetLastRect();
				if (lastRect.Contains(Event.current.mousePosition)) {
					DragAndDrop.AcceptDrag();

					var selectedObjects = DragAndDrop.objectReferences;

					//foreach (var selectedObject in selectedObjects) {
					//	listProperty.AddObject(selectedObject);
					//}
				}

			}
			#endregion

			property.serializedObject.ApplyModifiedProperties();

			EditorGUI.EndProperty();

		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
			var baseHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			var list = GetList(property);
			if (list != null) {
				return list.GetHeight() + baseHeight;
			}
			return base.GetPropertyHeight(property, label);
		}

	}
}