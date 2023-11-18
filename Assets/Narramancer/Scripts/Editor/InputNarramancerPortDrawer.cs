using System;
using UnityEditor;
using UnityEngine;

namespace Narramancer {

	[CustomPropertyDrawer(typeof(InputNarramancerPort))]
	public class InputNarramancerPortDrawer : PropertyDrawer {

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

			EditorGUI.BeginProperty(position, label, property);

			var typeProperty = property.FindPropertyRelative(NarramancerPort.TypeFieldName);
			var typeRect = new Rect(position.x, position.y, position.width * 0.45f, position.height);
			EditorGUI.PropertyField(typeRect, typeProperty, GUIContent.none);

			var nameProperty = property.FindPropertyRelative(NarramancerPort.NameFieldName);
			var nameRect = new Rect(position.x + position.width * 0.45f, position.y, position.width * 0.45f, position.height);
			nameProperty.stringValue = EditorGUI.TextField(nameRect, nameProperty.stringValue);

			var idProperty = property.FindPropertyRelative(NarramancerPort.IdFieldName);
			if (idProperty.stringValue.IsNullOrEmpty()) {
				idProperty.stringValue = Guid.NewGuid().ToString();
			}

			var passThroughProperty = property.FindPropertyRelative(InputNarramancerPort.PassThroughFieldName);
			var passThroughRect = new Rect(position.x + position.width * 0.9f, position.y, position.width * 0.1f, position.height);
			var labelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = position.width * 0.04f;
			passThroughProperty.boolValue = EditorGUI.Toggle(passThroughRect, new GUIContent(" P", "Pass the input through as an output"), passThroughProperty.boolValue );
			EditorGUIUtility.labelWidth = labelWidth;

			EditorGUI.EndProperty();
		}
	}
}