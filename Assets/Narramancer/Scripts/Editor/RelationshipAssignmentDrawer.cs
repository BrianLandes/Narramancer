using UnityEditor;
using UnityEngine;

namespace Narramancer {

	[CustomPropertyDrawer(typeof(RelationshipAssignment))]
	public class RelationshipAssignmentDrawer : PropertyDrawer {

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

			EditorGUI.BeginProperty(position, label, property);

			var scriptableObject = property.FindPropertyRelative(nameof(RelationshipAssignment.relationship));
			var objectRect = new Rect(position.x, position.y, position.width * 0.4f, position.height);
			scriptableObject.objectReferenceValue = EditorGUI.ObjectField(objectRect, GUIContent.none, scriptableObject.objectReferenceValue, typeof(RelationshipScriptableObject), false);

			var other = property.FindPropertyRelative(nameof(RelationshipAssignment.other));
			var otherRect = new Rect(position.x + position.width * 0.4f, position.y, position.width * 0.35f, position.height);
			other.objectReferenceValue = EditorGUI.ObjectField(otherRect, GUIContent.none, other.objectReferenceValue, typeof(NounScriptableObject), false);

			var sourceOrDestination = property.FindPropertyRelative(nameof(RelationshipAssignment.sourceOrDestination));
			var sourceOrDestinationRect = new Rect(position.x + position.width * 0.75f, position.y, position.width * 0.25f, position.height);
			sourceOrDestination.intValue = EditorGUI.Popup(sourceOrDestinationRect, sourceOrDestination.intValue, new[] { "Source", "Destination" });

			EditorGUI.EndProperty();
		}
	}
}