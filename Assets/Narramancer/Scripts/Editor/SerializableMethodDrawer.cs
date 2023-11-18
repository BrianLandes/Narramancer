
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Narramancer {

	[CustomPropertyDrawer(typeof(SerializableMethod))]
	public class SerializableMethodDrawer : PropertyDrawer {

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

			EditorGUI.BeginProperty(position, label, property);

			var labelPosition = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
			var propertyName = property.propertyPath.Nicify();
			EditorGUI.LabelField(labelPosition, propertyName);


			var buttonPosition = new Rect(position.x + labelPosition.width, position.y, position.width - labelPosition.width, position.height);
			var targetProperty = property.GetTargetObject<SerializableMethod>();

			var style = new GUIStyle(GUI.skin.button);
			style.alignment = TextAnchor.UpperLeft;
			if (GUI.Button(buttonPosition, new GUIContent(targetProperty.ToString(), targetProperty.ToolTip()), style)) {

				void ApplyMethod(MethodInfo methodInfo) {
					property.serializedObject.Update();
					targetProperty.SetTargetMethod(methodInfo);
					property.serializedObject.ApplyModifiedProperties();

					targetProperty.ApplyChanges();

				}

				Matrix4x4 originalMatrix = GUI.matrix;
				GUI.matrix = Matrix4x4.identity;

				var mousePosition = Event.current.mousePosition;
				var screenPosition = GUIUtility.GUIToScreenPoint(mousePosition);

				MethodSearchModalWindow window = ScriptableObject.CreateInstance(typeof(MethodSearchModalWindow)) as MethodSearchModalWindow;
				var types = targetProperty.LookupTypes ?? targetProperty.GetLookupTypes();
				window.SearchTypes(screenPosition, types, ApplyMethod);

				GUI.matrix = originalMatrix;
			}

			EditorGUI.EndProperty();

		}


	}
}