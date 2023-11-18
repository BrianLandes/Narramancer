using Narramancer.SerializableDictionary;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Narramancer {

	[CustomPropertyDrawer(typeof(Blackboard))]
	public class BlackboardDrawer : PropertyDrawer {

		SerializableDictionaryPropertyDrawer serializableDictionaryPropertyDrawer;

		string[] propertyNames = new[] { "ints", "bools", "floats", "strings", "unityObjects" };

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

			serializableDictionaryPropertyDrawer = serializableDictionaryPropertyDrawer!=null ? serializableDictionaryPropertyDrawer : new SerializableDictionaryPropertyDrawer();

			EditorGUI.BeginProperty(position, label, property);

			var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
			property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);
			if (property.isExpanded) { // TODO: animate this showing/hiding
				var y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
				EditorGUI.indentLevel++;

				foreach (var propertyName in propertyNames) {
					var relativeProperty = property.FindPropertyRelative(propertyName);
					if (relativeProperty != null) {
						var height = EditorGUI.GetPropertyHeight(relativeProperty);
						var rect = new Rect(position.x, y, position.width, height);

						var contents = new GUIContent($"{propertyName} Values");
						height = serializableDictionaryPropertyDrawer.GetPropertyHeight(relativeProperty, contents);

						serializableDictionaryPropertyDrawer.OnGUI(rect, relativeProperty, contents);

						y += height + EditorGUIUtility.standardVerticalSpacing;
					}
					
				}

				if (position.Contains(Event.current.mousePosition)) {
					switch (Event.current.type) {

						case EventType.DragUpdated:
							EditorGUI.LabelField(position, "Drag and Drop here");
							DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
							break;
						case EventType.DragPerform:
							DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
							DragAndDrop.AcceptDrag();

							var unityObjects = property.FindPropertyRelative("unityObjects");
							var keyArrayProperty = unityObjects.FindPropertyRelative(SerializableDictionaryPropertyDrawer.KeysFieldName);
							var valueArrayProperty = unityObjects.FindPropertyRelative(SerializableDictionaryPropertyDrawer.ValuesFieldName);
							foreach (var draggedObject in DragAndDrop.objectReferences.Cast<UnityEngine.Object>()) {

								int index = keyArrayProperty.arraySize;
								keyArrayProperty.InsertArrayElementAtIndex(index);
								valueArrayProperty.InsertArrayElementAtIndex(index);
								keyArrayProperty.GetArrayElementAtIndex(index).stringValue = draggedObject.name;
								valueArrayProperty.GetArrayElementAtIndex(index).objectReferenceValue = draggedObject;
							}
							break;
					}
				}

				EditorGUI.indentLevel--;
			}

			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {

			serializableDictionaryPropertyDrawer = serializableDictionaryPropertyDrawer!=null ? serializableDictionaryPropertyDrawer : new SerializableDictionaryPropertyDrawer();
			var result = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			if (property.isExpanded) {
				foreach (var propertyName in propertyNames) {
					var relativeProperty = property.FindPropertyRelative(propertyName);
					if (relativeProperty != null) {
						result += serializableDictionaryPropertyDrawer.GetPropertyHeight(relativeProperty, label);
						result += EditorGUIUtility.standardVerticalSpacing;
					}
					
				}
			}
			return result;
		}
	}
}