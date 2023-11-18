using UnityEditor;
using UnityEngine;

namespace Narramancer {

	[CustomPropertyDrawer(typeof(StatInstance))]
	public class StatInstanceDrawer : PropertyDrawer {

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

			EditorGUI.BeginProperty(position, label, property);

			var scriptableObject = property.FindPropertyRelative(AdjectiveInstance.AdjectiveFieldName);
			var objectRect = new Rect(position.x, position.y, position.width * 0.5f, position.height);
			scriptableObject.objectReferenceValue = EditorGUI.ObjectField(objectRect, GUIContent.none, scriptableObject.objectReferenceValue, typeof(StatScriptableObject), false);

			var value = property.FindPropertyRelative(StatInstance.ValueFieldname);
			var valueRect = new Rect(position.x + position.width * 0.5f, position.y, position.width * 0.5f, position.height);
			value.floatValue = EditorGUI.FloatField(valueRect, value.floatValue);

			EditorGUI.EndProperty();
		}
	}
}