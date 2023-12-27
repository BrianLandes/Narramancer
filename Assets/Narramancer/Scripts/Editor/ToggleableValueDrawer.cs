using UnityEditor;
using UnityEngine;

namespace Narramancer {
	[CustomPropertyDrawer(typeof(ToggleableValue<>))]
	[CustomPropertyDrawer(typeof(ToggleableFloat))]
	[CustomPropertyDrawer(typeof(ToggleableInt))]
	[CustomPropertyDrawer(typeof(ToggleableString))]
	[CanEditMultipleObjects]
	public class ToggleableValueDrawer : PropertyDrawer {

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

			EditorGUI.BeginProperty(position, label, property);

			//property.serializedObject.Update();

			var labelPosition = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
			var propertyName = property.propertyPath.Nicify();
			EditorGUI.LabelField(labelPosition, propertyName);

			EditorGUI.BeginChangeCheck();

			var activated = property.FindPropertyRelative(nameof(ToggleableFloat.activated));
			var activatedPosition = new Rect(position.x + labelPosition.width, position.y, 25, position.height);
			activated.boolValue = EditorGUI.Toggle(activatedPosition, activated.boolValue);

			if (activated.boolValue) {
				var value = property.FindPropertyRelative(nameof(ToggleableFloat.value));
				var valuePosition = new Rect(activatedPosition.x + activatedPosition.width, position.y, position.width - labelPosition.width - activatedPosition.width, position.height);
				EditorGUI.PropertyField(valuePosition, value, GUIContent.none);
			}

			if (EditorGUI.EndChangeCheck()) {
				property.serializedObject.ApplyModifiedProperties();
			}

			EditorGUI.EndProperty();
		}
	}
}