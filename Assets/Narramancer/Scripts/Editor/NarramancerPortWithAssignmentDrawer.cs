using System;
using UnityEditor;
using UnityEngine;

namespace Narramancer {

	[CustomPropertyDrawer(typeof(NarramancerPortWithAssignment))]
	public class NarramancerPortWithAssignmentDrawer : PropertyDrawer {

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

			EditorGUI.BeginProperty(position, label, property);

			var typeProperty = property.FindPropertyRelative(NarramancerPort.TypeFieldName);
			
			var typeRect = new Rect(position.x, position.y, position.width * 0.3f, position.height);
			EditorGUI.PropertyField(typeRect, typeProperty, GUIContent.none);

			var nameProperty = property.FindPropertyRelative(NarramancerPort.NameFieldName);
			var nameRect = new Rect(position.x + position.width * 0.3f, position.y, position.width * 0.375f, position.height);
			nameProperty.stringValue = EditorGUI.TextField(nameRect, nameProperty.stringValue);

			var assignmentProperty = property.FindPropertyRelative(NarramancerPortWithAssignment.AssignmentFieldName);
			var assignmentRect = new Rect(position.x + position.width * 0.7f, position.y, position.width * 0.3f, position.height);
			var assignmentTypeProperty = assignmentProperty.FindPropertyRelative(nameof(VariableAssignment.type));
			var typeTypeProperty = typeProperty.FindPropertyRelative(SerializableType.TypeFieldName);
			assignmentTypeProperty.stringValue = VariableAssignment.TypeNameToVariableAssignmentType(typeTypeProperty.stringValue);
			EditorDrawerUtilities.VariableAssignmentField(assignmentRect, assignmentProperty, GUIContent.none);

			var idProperty = property.FindPropertyRelative(NarramancerPort.IdFieldName);
			if (idProperty.stringValue.IsNullOrEmpty() ) {
				idProperty.stringValue = Guid.NewGuid().ToString();
			}

			EditorGUI.EndProperty();
		}
	}
}