using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Narramancer {

	[CustomPropertyDrawer(typeof(SerializableType))]
	public class SerializableTypeDrawer : PropertyDrawer {

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

			EditorGUI.BeginProperty(position, label, property);

			var startX = 0f;
			if (label != GUIContent.none) {
				var labelPosition = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);
				var propertyName = label != null ? label.text : property.propertyPath.Split('.').Last().Nicify();
				EditorGUI.LabelField(labelPosition, propertyName);
				startX = labelPosition.width;
			}


			var buttonPosition = new Rect(position.x + startX, position.y, position.width - startX, position.height);

			var typeNameProperty = property.FindPropertyRelative("typeName");

			var listProperty = property.FindPropertyRelative("list");

			var typeName = typeNameProperty.stringValue;
			var buttonText = "";
			var buttonTooltip = "";
			if (typeName.IsNullOrEmpty()) {
				buttonText = "(No Type)";
				buttonTooltip = "Please select a type";
			}
			else {
				var type = AssemblyUtilities.GetType(typeName);
				if (type != null) {
					var name = type.Name;
					if (EditorDrawerUtilities.primitiveTypes.TryGetValue(type, out var primitiveName)) {
						name = primitiveName;
					}
					if (listProperty.boolValue) {
						buttonText = $"List<{name}>";
					}
					else {
						buttonText = name;
					}
					buttonTooltip = type.FullName;
				}
				if (type != null && typeNameProperty.stringValue != type.AssemblyQualifiedName) {
					typeNameProperty.stringValue = type.AssemblyQualifiedName;
					property.serializedObject.ApplyModifiedProperties();
				}
			}

			var style = new GUIStyle(GUI.skin.button);
			style.alignment = TextAnchor.UpperLeft;
			if (GUI.Button(buttonPosition, new GUIContent(buttonText, buttonTooltip), style)) {

				void ApplyType(Type type) {
					property.serializedObject.Update();
					typeNameProperty.stringValue = type.AssemblyQualifiedName;
					property.serializedObject.ApplyModifiedProperties();

					var targetProperty = property?.GetTargetObject<SerializableType>();
					targetProperty?.ApplyChanges();

				}

				void AddListButtons(GenericMenu menu) {
					var targetProperty = property?.GetTargetObject<SerializableType>();
					if (targetProperty != null && targetProperty.canBeList) {
						var className = AssemblyUtilities.GetClassName(typeName);

						var listText = "List of " + className;
						var notListText = "Single Element of " + className;
						if (listProperty.boolValue) {
							menu.AddDisabledItem(new GUIContent(listText), true);
						}

						var text = listProperty.boolValue ? notListText : listText;

						menu.AddItem(new GUIContent(text), false, () => {
							property.serializedObject.Update();
							listProperty.boolValue = !listProperty.boolValue;
							property.serializedObject.ApplyModifiedProperties();

							var targetObject = property.serializedObject.targetObject;
							var parentType = targetObject.GetType();
							var fieldInfo = parentType.GetField(property.propertyPath, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
							if (fieldInfo != null) {
								var targetProperty = fieldInfo.GetValue(targetObject) as SerializableType;
								targetProperty.ApplyChanges();
							}
							// TODO: account for SerializableTypes that are part of lists (eg: Story.GlobalVariables)
						});

						if (!listProperty.boolValue) {
							menu.AddDisabledItem(new GUIContent(notListText), true);
						}

						menu.AddSeparator(string.Empty);
					}
					
				}

				var targetProperty = property?.GetTargetObject<SerializableType>();
				EditorDrawerUtilities.ShowTypeSelectionPopup(ApplyType, onBeforeTypeItems: AddListButtons, typeFilter: targetProperty?.typeFilter);
			}

			EditorGUI.EndProperty();

		}

	}
}
