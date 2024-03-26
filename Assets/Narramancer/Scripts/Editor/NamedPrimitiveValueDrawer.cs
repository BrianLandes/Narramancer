using System;
using UnityEditor;
using UnityEngine;

namespace Narramancer {

	[CustomPropertyDrawer(typeof(NamedPrimitiveValue), false)]
	[CanEditMultipleObjects]
	public class NamedPrimitiveValueDrawer : PropertyDrawer {

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

			property.serializedObject.Update();

			EditorGUI.BeginProperty(position, label, property);

			var nameRect = new Rect(position.x, position.y, position.width * 0.5f, position.height);
			var nameProperty = property.FindPropertyRelative(nameof(NamedPrimitiveValue.name));


			EditorGUI.BeginChangeCheck();
			property.serializedObject.Update();

			EditorGUI.PropertyField(nameRect, nameProperty, GUIContent.none);

			if (EditorGUI.EndChangeCheck()) {
				property.serializedObject.ApplyModifiedProperties();
			}


			var buttonWidth = 30f;

			var valueRect = new Rect(position.x + nameRect.width + 4, position.y, position.width * 0.5f - buttonWidth - 4, position.height);
			var valueProperty = property.FindPropertyRelative(nameof(NamedPrimitiveValue.value));
			EditorGUI.PropertyField(valueRect, valueProperty, GUIContent.none);

			var buttonRect = new Rect(valueRect.x + valueRect.width, position.y, buttonWidth, position.height);
			if (GUI.Button(buttonRect, "Type")) {
				EditorDrawerUtilities.ShowTypeSelectionPopup(type => {
					property.serializedObject.Update();
					var typeProperty = valueProperty.FindPropertyRelative(nameof(SerializablePrimitive.type));
					typeProperty.stringValue = SerializablePrimitive.TypeToString(type);
					property.serializedObject.ApplyModifiedProperties();
				},
				typeFilter: SerializablePrimitive.IsSupportedType );
			}

			EditorGUI.EndProperty();


		}
	}
}