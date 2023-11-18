using System;
using UnityEditor;
using UnityEngine;

namespace Narramancer {

	[CustomPropertyDrawer(typeof(NarramancerPort))]
	public class NarramancerPortDrawer : PropertyDrawer {

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

			EditorGUI.BeginProperty(position, label, property);

			var typeProperty = property.FindPropertyRelative(NarramancerPort.TypeFieldName);
			var typeRect = new Rect(position.x, position.y, position.width * 0.5f, position.height);
			EditorGUI.PropertyField(typeRect, typeProperty, GUIContent.none);

			var nameProperty = property.FindPropertyRelative(NarramancerPort.NameFieldName);
			var nameRect = new Rect(position.x + position.width * 0.5f, position.y, position.width * 0.5f, position.height);
			nameProperty.stringValue = EditorGUI.TextField(nameRect, nameProperty.stringValue);

			var idProperty = property.FindPropertyRelative(NarramancerPort.IdFieldName);
			if (idProperty.stringValue.IsNullOrEmpty() ) {
				idProperty.stringValue = Guid.NewGuid().ToString();
			}

			EditorGUI.EndProperty();
		}
	}
}