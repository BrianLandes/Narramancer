
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Narramancer {
	[CustomPropertyDrawer(typeof(NounScriptableObjectList))]
	public class NounScriptableObjectListDrawer : PropertyDrawer {

		ReorderableList list;

		bool openObjectPicker = false;
		const int NOUN_PICKER = 1 << 1;

		GUIStyle objectStyle;
		GUIContent objectContent;

		UnityEngine.Object draggedElement;

		string search;

		void DrawObject(Rect position, UnityEngine.Object @object) {
			var name = @object.name;

			using (EditorDrawerUtilities.Color()) {

				objectContent.text = name;
#if UNITY_2021_3_OR_NEWER
				var customIcon = EditorGUIUtility.GetIconForObject(@object);
				if (customIcon != null) {
					objectContent.image = customIcon;
				}
#endif
				
				if (GUI.Button(position, objectContent, objectStyle)) {
					EditorGUIUtility.PingObject(@object);
					Selection.activeObject = @object;

				}

				//if (Event.current.type == EventType.MouseDown) {
				//	if (position.Contains(Event.current.mousePosition)) {
				//		draggedElement = @object;
				//	}
				//}

			}
		}

		private ReorderableList GetList(SerializedProperty property) {
			if (list == null) {
				var listProperty = property.FindPropertyRelative(nameof(NounScriptableObjectList.list));
				list = new ReorderableList(property.serializedObject, listProperty, true, false, true, true);

				objectStyle = new GUIStyle(EditorStyles.objectField);
				objectStyle.border = new RectOffset();
				objectStyle.padding = new RectOffset();

				objectContent = new GUIContent(EditorGUIUtility.IconContent("d_ScriptableObject Icon"));

				list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
					var element = listProperty.GetArrayElementAtIndex(index);
					var elementObject = element.objectReferenceValue;
					if (elementObject == null) {
						return;
					}
					EditorGUIUtility.SetIconSize(Vector2.one * 20);

					var objectRect = new Rect(rect.x, rect.y + 1, rect.width, rect.height - 2);
					DrawObject(objectRect, elementObject);

					EditorGUIUtility.SetIconSize(Vector2.zero);
				};

				list.onAddCallback = list => {
					var menu = new GenericMenu();

					menu.AddItem(new GUIContent("Create new asset..."), false, () => {
						var path = EditorUtility.SaveFilePanelInProject("Create New Character", "Character", "asset", "Choose a directory and name", "Assets/Scriptable Objects/Characters");
						if (path.IsNotNullOrEmpty()) {
							var newCharacter = ScriptableObject.CreateInstance<NounScriptableObject>();
							newCharacter.name = Path.GetFileNameWithoutExtension(path);
							AssetDatabase.CreateAsset(newCharacter, path);
							AssetDatabase.Refresh();
							AssetDatabase.SaveAssets();

							listProperty.serializedObject.Update();
							listProperty.AddObject(newCharacter);
							listProperty.serializedObject.ApplyModifiedProperties();
						}
					});

					menu.AddItem(new GUIContent("Add Existing..."), false, () => {
						openObjectPicker = true;
						EditorGUIUtility.ShowObjectPicker<NounScriptableObject>(null, false, "", NOUN_PICKER);
					});

					menu.ShowAsContext();
				};

			}
			return list;
		}



		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

			#region Allow DragAndDropping Elements From This List

			//if (Event.current.type == EventType.MouseDrag) {
			//	if (draggedElement != null) {
			//		DragAndDrop.PrepareStartDrag();
			//		DragAndDrop.StartDrag("Dragging " + draggedElement.name);
			//		DragAndDrop.objectReferences = new UnityEngine.Object[] { draggedElement };
			//		draggedElement = null;
			//	}
			//}

			//if (Event.current.type == EventType.MouseUp || Event.current.type == EventType.MouseLeaveWindow) {
			//	draggedElement = null;
			//}

			#endregion

			property.serializedObject.Update();

			EditorGUI.BeginProperty(position, label, property);

			var listProperty = property.FindPropertyRelative(nameof(NounScriptableObjectList.list));

			#region Search Bar
			var searchIconRect = new Rect(position.x, position.y, 30, EditorGUIUtility.singleLineHeight);
			var searchCloseRect = new Rect(position.x + position.width - 30, position.y, 30, EditorGUIUtility.singleLineHeight);
			var searchTextRect = new Rect(position.x + searchIconRect.width, position.y, position.width - searchIconRect.width - searchCloseRect.width, EditorGUIUtility.singleLineHeight);
			EditorGUI.LabelField(searchIconRect, EditorGUIUtility.IconContent("d_Search Icon") );
			search = EditorGUI.TextField(searchTextRect, search);
			if (GUI.Button(searchCloseRect, EditorGUIUtility.IconContent("winbtn_win_close"))) {
				EditorGUI.FocusTextInControl("");
				search = string.Empty;
			}
			#endregion

			if ( search.IsNotNullOrEmpty() ) {

				var searchLower = search.ToLower();
				var searchTerms = searchLower.Split(' ');

				bool ContainsAnySearchTerms(UnityEngine.Object value) {
					var fullName = value.name.ToLower();
					if (searchTerms.All(term => fullName.Contains(term))) {
						return true;
					}
					return false;
				}

				var objectRect = new Rect(position.x, searchTextRect.y + searchTextRect.height, position.width, EditorGUIUtility.singleLineHeight);

				EditorGUIUtility.SetIconSize(Vector2.one * 20);

				for( var ii = 0; ii < listProperty.arraySize; ii++) {
					var element = listProperty.GetArrayElementAtIndex(ii);
					var elementObject = element.objectReferenceValue;
					if (elementObject == null) {
						continue;
					}

					// TODO: search properties, stats, and relationships attached to the nouns
					if (!ContainsAnySearchTerms(elementObject )) {
						continue;
					}

					DrawObject(objectRect, elementObject);
					objectRect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

				}

				EditorGUIUtility.SetIconSize(Vector2.zero);
			}
			else {
				var listRect = new Rect(position.x, position.y + searchTextRect.height + EditorGUIUtility.standardVerticalSpacing,
					position.width, position.height - searchTextRect.height - EditorGUIUtility.standardVerticalSpacing);
				var list = GetList(property);
				list.DoList(listRect);
			}

			#region Object Picker
			if (openObjectPicker && Event.current.commandName == "ObjectSelectorSelectionDone") {
				openObjectPicker = false;
				var selectedObject = EditorGUIUtility.GetObjectPickerObject();

				switch (EditorGUIUtility.GetObjectPickerControlID()) {
					case NOUN_PICKER:
						listProperty.AddObject(selectedObject);
						break;
				}
			}
			#endregion

			#region Accept DragAndDrop
			if (Event.current.type == EventType.DragUpdated) {
				DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
			}
			if (Event.current.type == EventType.DragPerform) {
				DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
				var lastRect = GUILayoutUtility.GetLastRect();
				if (lastRect.Contains(Event.current.mousePosition) && DragAndDrop.objectReferences.Any( @object=> @object is NounScriptableObject) ) {
					DragAndDrop.AcceptDrag();

					var selectedObjects = DragAndDrop.objectReferences.Where(@object => @object is NounScriptableObject);

					foreach (var selectedObject in selectedObjects) {
						listProperty.AddObject(selectedObject);
					}
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