
//using System.Collections.Generic;
//using System.Linq;
//using UnityEditor;
//using UnityEngine;
//using XNodeEditor;

//namespace Narramancer {
//	[CustomNodeEditor(typeof(GetVerbVariableNode))]
//	public class GetVerbVariableNodeEditor : NodeEditor {

//		private static Dictionary<Color, Texture2D> textureTable = new Dictionary<Color, Texture2D>();
//		private static Texture2D dropDownIcon = null;

//		public override void OnBodyGUI() {
//			var getGraphVariableNode = target as GetVerbVariableNode;
//			var graph = getGraphVariableNode.graph as VerbGraph;

//			if (graph == null) {
//				return;
//			}


//			var inputId = serializedObject.FindProperty("inputId");
//			if (inputId.stringValue.IsNullOrEmpty() && graph.Inputs.Any()) {
//				serializedObject.Update();
//				inputId.stringValue = graph.Inputs.First().Id;
//				serializedObject.ApplyModifiedProperties();

//				getGraphVariableNode.ApplyChanges();
//			}

//			var buttonText = string.Empty;

//			var correspondingInput = graph.Inputs.FirstOrDefault(input => input.Id.Equals(inputId.stringValue));
//			if (correspondingInput != null) {
//				buttonText = correspondingInput.ToString();
//			}
//			else {
//				buttonText = "(None)";
//			}

//			var nodePort = correspondingInput != null ? getGraphVariableNode.GetOutputPort("value") : null;
//			if (correspondingInput != null && (nodePort == null || nodePort.ValueType != correspondingInput.Type)) {
//				getGraphVariableNode.ApplyChanges();
//				nodePort = getGraphVariableNode.GetOutputPort(correspondingInput.Name);
//			}
//			var dropDownStyle = new GUIStyle(EditorStyles.miniPullDown);
//			if (nodePort != null) {
//				var color = NodeEditorPreferences.GetTypeColor(nodePort.ValueType);
//				if (!textureTable.TryGetValue(color, out var backgroundTexture) || backgroundTexture == null) {
//					backgroundTexture = new Texture2D(1,1);
//					backgroundTexture.SetPixel(0, 0, color);
//					backgroundTexture.Apply();
//					textureTable[color] = backgroundTexture;
//				}
//				dropDownStyle.normal.background = backgroundTexture;
//				dropDownStyle.active.background = backgroundTexture;
//				dropDownStyle.focused.background = backgroundTexture;
//				if (color.grayscale > 0.5f ) {
//					dropDownStyle.active.textColor = Color.black;
//					dropDownStyle.normal.textColor = Color.black;
//				}
//			}

//			if (EditorGUILayout.DropdownButton(new GUIContent(buttonText, buttonText), FocusType.Passive, dropDownStyle)) {
//				GenericMenu context = new GenericMenu();

//				foreach (var input in graph.Inputs) {
//					context.AddItem(new GUIContent(input.ToString()), input.Id == inputId.stringValue, () => {
//						serializedObject.Update();
//						correspondingInput = input;
//						inputId.stringValue = correspondingInput.Id;
//						serializedObject.ApplyModifiedProperties();

//						getGraphVariableNode.ApplyChanges();
//					});
//				}

//				if (context.GetItemCount() == 0) {
//					context.AddDisabledItem(new GUIContent("(No valid values)"));
//				}

//				Matrix4x4 originalMatrix = GUI.matrix;
//				GUI.matrix = Matrix4x4.identity;
//				context.ShowAsContext();
//				GUI.matrix = originalMatrix;
//			}

//			dropDownIcon = dropDownIcon!=null ? dropDownIcon : EditorGUIUtility.FindTexture("icon dropdown@2x");
//			var lastRect = GUILayoutUtility.GetLastRect();
//			var iconRect = new Rect(lastRect.x + lastRect.width - 16, lastRect.y, 16, lastRect.height);
//			GUI.DrawTexture(iconRect, dropDownIcon);

//			if (nodePort != null) {

//				// this will cause the port to be drawn on the same line as the DropdownButton
//				EditorGUILayout.Space(-EditorGUIUtility.singleLineHeight - EditorGUIUtility.standardVerticalSpacing);

//				NodeEditorGUILayout.PortField(GUIContent.none, nodePort, serializedObject);
//			}

//		}
//	}
//}