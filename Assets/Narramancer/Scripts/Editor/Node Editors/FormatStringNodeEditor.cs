using System.Linq;
using UnityEditor;
using UnityEngine;
using XNode;
using XNodeEditor;

namespace Narramancer {

	[CustomNodeEditor(typeof(FormatStringNode))]
	public class FormatStringNodeEditor : ResizableNodeEditor {

		GUIContent menuContent;
		GUIContent addInputContent;

		public override void OnBodyGUI() {

			var textProperty = serializedObject.FindProperty(FormatStringNode.TextFieldName);
			NodeEditorGUILayout.PropertyField(textProperty);

			menuContent = menuContent != null ? menuContent: EditorGUIUtility.IconContent("_Menu");

			foreach ( var input in target.DynamicInputs) {

				EditorGUILayout.BeginHorizontal();

				NodeEditorGUILayout.PortField(new GUIContent(input.fieldName), input, serializedObject );

				if (GUILayout.Button(menuContent, GUILayout.Width(30))) {

					GenericMenu context = new GenericMenu();

					context.AddItem(new GUIContent("Delete"), false, () => {
						serializedObject.ApplyModifiedProperties();
						target.RemoveDynamicPort(input);
						serializedObject.Update();
					});

					var mousePosition = Event.current.mousePosition;
					var screenPosition = GUIUtility.GUIToScreenPoint(mousePosition);

					context.AddItem(new GUIContent("Rename"), false, () => {
						EnterTextModalWindow.Show(screenPosition, input.fieldName, newName => {
							if (target.DynamicInputs.Any(x => x!=input && x.fieldName.Equals(newName))) {
								Debug.LogError("An input already exists with the name " + newName, target);
							}
							else {
								serializedObject.ApplyModifiedProperties();
								var newInput = target.AddDynamicInput(input.ValueType, XNode.Node.ConnectionType.Override, XNode.Node.TypeConstraint.Inherited, newName);
								input.SwapConnections(newInput);
								target.RemoveDynamicPort(input);
								serializedObject.Update();
							}
							
						});
					});

					Matrix4x4 originalMatrix = GUI.matrix;
					GUI.matrix = Matrix4x4.identity;
					context.ShowAsContext();
					GUI.matrix = originalMatrix;
				}

				EditorGUILayout.EndHorizontal();
			}

			addInputContent = addInputContent!=null ? addInputContent : new GUIContent(EditorGUIUtility.IconContent("CreateAddNew"));
			addInputContent.text = "Add New Input";

			if (GUILayout.Button(addInputContent)) {

				EditorDrawerUtilities.ShowTypeSelectionPopup(
					type => {
						serializedObject.ApplyModifiedProperties();
						var otherInputCount = target.DynamicInputs.Count(x => x.fieldName.Contains(type.Name));
						var name = $"{type.Name}{(otherInputCount>0? otherInputCount.ToString() :string.Empty)}";
						var newInput = target.AddDynamicInput(type, XNode.Node.ConnectionType.Override, XNode.Node.TypeConstraint.Inherited, name);
						serializedObject.Update();
					}
				);
			}

			var resultPort = target.GetOutputPort(FormatStringNode.ResultFieldName);
			NodeEditorGUILayout.PortField(resultPort, serializedObject);

		}
	}
}