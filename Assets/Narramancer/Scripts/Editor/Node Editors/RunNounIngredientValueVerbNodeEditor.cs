
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;

namespace Narramancer {
	[CustomNodeEditor(typeof(RunNounIngredientValueVerbNode))]
	public class RunNounIngredientValueVerbNodeEditor : AbstractInstanceInputNodeEditor {

		public override void OnTailGUI() {

			var runActionVerbNode = target as RunNounIngredientValueVerbNode;

			serializedObject.Update();
			var ingredientTypeProperty = serializedObject.FindProperty(RunNounIngredientValueVerbNode.IngredientTypeFieldName);
			EditorGUILayout.PropertyField(ingredientTypeProperty);
			serializedObject.ApplyModifiedProperties();

			var ingredientType = runActionVerbNode.IngredientType;

			if (ingredientType != null) {

				var graphNameProperty = serializedObject.FindProperty(RunNounIngredientValueVerbNode.VerbNameFieldName);
				var popupValues = new List<string>();
				var selectedIndex = -1;

				var fields = ingredientType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.DeclaredOnly);

				foreach (var field in fields) {
					if (field.FieldType.IsAssignableFrom(typeof(ValueVerb))) {
						popupValues.Add(field.Name);
						if (field.Name.Equals(graphNameProperty.stringValue)) {
							selectedIndex = popupValues.Count - 1;
						}
					}
				}

				selectedIndex = EditorGUILayout.Popup("Value Verb", selectedIndex, popupValues.ToArray());
				if (selectedIndex >= 0 && selectedIndex < popupValues.Count && !graphNameProperty.stringValue.Equals(popupValues[selectedIndex])) {
					serializedObject.Update();
					graphNameProperty.stringValue = popupValues[selectedIndex];
					serializedObject.ApplyModifiedProperties();
					runActionVerbNode.UpdatePorts();
				}

			}

		}
	}
}