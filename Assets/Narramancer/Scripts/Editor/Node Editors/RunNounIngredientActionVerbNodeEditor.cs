
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Narramancer {
	[CustomNodeEditor(typeof(RunNounIngredientActionVerbNode))]
	public class RunNounIngredientActionVerbNodeEditor : AbstractInstanceInputChainedRunnableNodeEditor {

		public override void OnBaseBodyGUI() {

			var runActionVerbNode = target as RunNounIngredientActionVerbNode;

			serializedObject.Update();
			var ingredientTypeProperty = serializedObject.FindProperty(RunNounIngredientActionVerbNode.IngredientTypeFieldName);
			EditorGUILayout.PropertyField(ingredientTypeProperty);
			serializedObject.ApplyModifiedProperties();

			var ingredientType = runActionVerbNode.IngredientType;

			if (ingredientType != null) {
				
				var graphNameProperty = serializedObject.FindProperty(RunNounIngredientActionVerbNode.VerbNameFieldName);
				var popupValues = new List<string>();
				var selectedIndex = -1;

				var fields = ingredientType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.DeclaredOnly);

				foreach (var field in fields) {
					if (field.FieldType.IsAssignableFrom(typeof(ActionVerb))) {
						popupValues.Add(field.Name);
						if (field.Name.Equals(graphNameProperty.stringValue)) {
							selectedIndex = popupValues.Count - 1;
						}
					}
				}

				//var methods = ingredientType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.DeclaredOnly);

				//foreach (var method in methods) {
				//	if (!method.GetParameters().Any() && !method.IsGenericMethod && method.ReturnType.IsAssignableFrom(typeof(ActionVerb))) {
				//		popupValues.Add(method.Name);
				//		if (method.Name.Equals(graphNameProperty.stringValue)) {
				//			selectedIndex = popupValues.Count - 1;
				//		}
				//	}
				//}

				selectedIndex = EditorGUILayout.Popup("Action Verb", selectedIndex, popupValues.ToArray());
				if (selectedIndex >= 0 && selectedIndex < popupValues.Count && !graphNameProperty.stringValue.Equals(popupValues[selectedIndex])) {
					serializedObject.Update();
					graphNameProperty.stringValue = popupValues[selectedIndex];
					serializedObject.ApplyModifiedProperties();
					runActionVerbNode.UpdatePorts();
				}
				
			}

			base.OnBaseBodyGUI();

		}
	}
}