using System;
using UnityEditor;
using UnityEngine;

namespace Narramancer {

	[CustomPropertyDrawer(typeof(NamedPrimitiveValue), false)]
	[CanEditMultipleObjects]
	public class NamedPrimitiveValueDrawer : PropertyDrawer {

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

			EditorGUI.BeginProperty(position, label, property);

			property.serializedObject.Update();

			EditorGUI.BeginChangeCheck();

			var nameRect = new Rect(position.x, position.y, position.width * 0.5f, position.height);
			var nameProperty = property.FindPropertyRelative(nameof(NamedPrimitiveValue.name));
			EditorGUI.PropertyField(nameRect, nameProperty, GUIContent.none);

			var buttonWidth = 30f;

			var valueRect = new Rect(position.x + position.width * 0.5f, position.y, position.width * 0.5f - buttonWidth, position.height);
			var valueProperty = property.FindPropertyRelative(nameof(NamedPrimitiveValue.value));
			EditorGUI.PropertyField(valueRect, valueProperty, GUIContent.none);

			var buttonRect = new Rect(valueRect.x + valueRect.width, position.y, buttonWidth, position.height);
			if ( GUI.Button(buttonRect, "Type")) {
				EditorDrawerUtilities.ShowTypeSelectionPopup(type => {
					property.serializedObject.Update();
					var typeProperty = valueProperty.FindPropertyRelative(nameof(SerializablePrimitive.type));
					typeProperty.stringValue = SerializablePrimitive.TypeToString(type);
					property.serializedObject.ApplyModifiedProperties();
				});
			}

			if (EditorGUI.EndChangeCheck()) {
				property.serializedObject.ApplyModifiedProperties();
			}

			EditorGUI.EndProperty();
		}
	}
}