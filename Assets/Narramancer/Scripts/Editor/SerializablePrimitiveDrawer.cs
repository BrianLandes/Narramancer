using System;
using UnityEditor;
using UnityEngine;

namespace Narramancer {

	[CustomPropertyDrawer(typeof(SerializablePrimitive), false)]
	[CanEditMultipleObjects]
	public class SerializablePrimitiveDrawer : PropertyDrawer {

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

			EditorGUI.BeginProperty(position, label, property);

			property.serializedObject.Update();

			EditorGUI.BeginChangeCheck();

			var typeValue = property.FindPropertyRelative(nameof(SerializablePrimitive.type));
			switch (typeValue.stringValue) {
				case "int":
					var intValue = property.FindPropertyRelative(nameof(SerializablePrimitive.intValue));
					intValue.intValue = EditorGUI.IntField(position, label, intValue.intValue);
					break;
				case "bool":
					var boolValue = property.FindPropertyRelative(nameof(SerializablePrimitive.boolValue));
					boolValue.boolValue = EditorGUI.Toggle(position, label, boolValue.boolValue);
					break;
				case "float":
					var floatValue = property.FindPropertyRelative(nameof(SerializablePrimitive.floatValue));
					floatValue.floatValue = EditorGUI.FloatField(position, label, floatValue.floatValue);
					break;
				case "string":
					var stringValue = property.FindPropertyRelative(nameof(SerializablePrimitive.stringValue));
					stringValue.stringValue = EditorGUI.TextField(position, label, stringValue.stringValue);
					break;
				case "color":
					var colorValue = property.FindPropertyRelative(nameof(SerializablePrimitive.color));
					colorValue.colorValue = EditorGUI.ColorField(position, label, colorValue.colorValue);
					break;
				case "vector2":
					var vector2Value = property.FindPropertyRelative(nameof(SerializablePrimitive.vector2));
					vector2Value.vector2Value = EditorGUI.Vector2Field(position, label, vector2Value.vector2Value);
					break;
				case "vector3":
					var vector3Value = property.FindPropertyRelative(nameof(SerializablePrimitive.vector3));
					vector3Value.vector3Value = EditorGUI.Vector3Field(position, label, vector3Value.vector3Value);
					break;
				case "":
					using (new EditorGUI.DisabledScope()) {
						EditorGUI.LabelField(position, "(No Type)");
					}
					break;
				default:
					var objectType = Type.GetType(typeValue.stringValue);
					var allowSceneObjects = typeof(GameObject).IsAssignableFrom(objectType) || typeof(Component).IsAssignableFrom(objectType);
					var objectValue = property.FindPropertyRelative(nameof(SerializablePrimitive.objectValue));

					objectValue.objectReferenceValue = EditorGUI.ObjectField(position, label, objectValue.objectReferenceValue, objectType, allowSceneObjects);

					break;
			}

			if (EditorGUI.EndChangeCheck()) {
				property.serializedObject.ApplyModifiedProperties();
			}

			EditorGUI.EndProperty();
		}
	}
}