
using System;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Narramancer {
	[CustomPropertyDrawer(typeof(NarramancerPortWithAssignmentList))]
	public class NarramancerPortWithAssignmentListDrawer : PropertyDrawer {
		ReorderableList list;

		private ReorderableList GetList(SerializedProperty property) {
			if (list == null) {
				var listProperty = property.FindPropertyRelative(nameof(NarramancerPortWithAssignmentList.list));
				list = new ReorderableList(property.serializedObject, listProperty, true, true, true, true);
				list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
					var element = list.serializedProperty.GetArrayElementAtIndex(index);
					EditorGUI.PropertyField(rect, element, GUIContent.none);
				};
				list.headerHeight = EditorGUIUtility.singleLineHeight * 2f;
				list.drawHeaderCallback = (rect) => {
					var name = property.propertyPath.Nicify();
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

				list.onAddCallback = list => {
					EditorDrawerUtilities.ShowTypeSelectionPopup(type => {
						listProperty.InsertArrayElementAtIndex(listProperty.arraySize);
						var newElement = listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1);
						var typeProperty = newElement.FindPropertyRelative(NarramancerPort.TypeFieldName);
						var typeTypeProperty = typeProperty.FindPropertyRelative(SerializableType.TypeFieldName);
						typeTypeProperty.stringValue = type.AssemblyQualifiedName;
						var nameProperty = newElement.FindPropertyRelative(NarramancerPort.NameFieldName);
						nameProperty.stringValue = type.Name.Uncapitalize();
						property.serializedObject.ApplyModifiedProperties();
					});

				};

			}
			return list;
		}



		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			var list = GetList(property);

			property.serializedObject.Update();

			EditorGUI.BeginProperty(position, label, property);

			list.DoList(position);

			if (Event.current.type == EventType.Layout) {
				EditorGUILayout.Space(position.height);
			}


			var targetObject = property.GetTargetObject<NarramancerPortWithAssignmentList>();

			targetObject?.list.EnsurePortsHaveUniqueIds();

			#region Accept DragAndDrop
			if (Event.current.type == EventType.DragUpdated) {
				DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
			}
			if (Event.current.type == EventType.DragPerform) {
				DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
				var lastRect = GUILayoutUtility.GetLastRect();
				if (lastRect.Contains(Event.current.mousePosition)) {
					DragAndDrop.AcceptDrag();

					var listProperty = property.FindPropertyRelative(nameof(NarramancerPortWithAssignmentList.list));

					var selectedObjects = DragAndDrop.objectReferences;

					void CreateNewVariable(Type type, string name, UnityEngine.Object @object) {
						property.serializedObject.Update();

						listProperty.InsertArrayElementAtIndex(listProperty.arraySize);
						var newElement = listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1);
						var typeProperty = newElement.FindPropertyRelative(NarramancerPort.TypeFieldName);
						var typeTypeProperty = typeProperty.FindPropertyRelative(SerializableType.TypeFieldName);
						typeTypeProperty.stringValue = type.AssemblyQualifiedName;
						var nameProperty = newElement.FindPropertyRelative(NarramancerPort.NameFieldName);
						nameProperty.stringValue = name;

						var assignmentProperty = newElement.FindPropertyRelative(NarramancerPortWithAssignment.AssignmentFieldName);
						var objectValue = assignmentProperty.FindPropertyRelative(nameof(VariableAssignment.objectValue));

						objectValue.objectReferenceValue = @object;
						property.serializedObject.ApplyModifiedProperties();
					}

					foreach (var selectedObject in selectedObjects.Where(x =>typeof(Component).IsAssignableFrom(x.GetType()))) {
						var type = selectedObject.GetType();

						CreateNewVariable(type, $"{selectedObject.name}.{type.Name}", selectedObject);
					}

					

					foreach (var selectedObject in selectedObjects.Where(x => typeof(GameObject).IsAssignableFrom(x.GetType())).Cast<GameObject>()) {

						// TODO: figure out how to get this context menu to work
						// Adding an element through menu only exists for a frame, can't figure out
						//var menu = new GenericMenu();

						//menu.AddItem(new GUIContent("GameObject"), false, () => {

							CreateNewVariable(typeof(GameObject), selectedObject.name, selectedObject);

						//});

						foreach (var component in selectedObject.GetComponents<Component>()) {
							var type = component.GetType();
							//menu.AddItem(new GUIContent(type.Name.Nicify()), false, () => {

								CreateNewVariable(type, $"{selectedObject.name}.{type.Name}", component);

							//});
						}

						//menu.ShowAsContext();
					}

					
				}

			}
			#endregion

			property.serializedObject.ApplyModifiedProperties();

			EditorGUI.EndProperty();

		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
			var list = GetList(property);
			if (list != null) {

				return list.GetHeight();
			}
			return base.GetPropertyHeight(property, label);
		}

	}
}