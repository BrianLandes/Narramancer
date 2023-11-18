using UnityEditor;
using UnityEngine;

namespace Narramancer {

	[CustomPropertyDrawer(typeof(PropertyInstance))]
	public class PropertyInstanceDrawer : PropertyDrawer {

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

			EditorGUI.BeginProperty(position, label, property);

			var scriptableObject = property.FindPropertyRelative(AdjectiveInstance.AdjectiveFieldName);
			var objectRect = new Rect(position.x, position.y, position.width, position.height);
			scriptableObject.objectReferenceValue = EditorGUI.ObjectField(objectRect, GUIContent.none, scriptableObject.objectReferenceValue, typeof(PropertyScriptableObject), false);

			EditorGUI.EndProperty();
		}
	}
}