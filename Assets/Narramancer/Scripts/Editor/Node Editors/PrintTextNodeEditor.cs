
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Narramancer {
	[CustomNodeEditor(typeof(PrintTextNode))]
	public class PrintTextNodeEditor : ChainedRunnableNodeEditor {

		private static GUIStyle editorTextStyle;

		private Vector2 scrollPos;

		private static Texture2D resizeIcon;

		private bool dragging = false;
		private float lastDragX = 0f;
		private float lastDragY = 0f;
		private int startingWidth = 0;
		private int startingHeight = 0;

		TextEditor textEditor;

		Color textColor = Color.blue;

		GUIContent menuContent;
		GUIContent addInputContent;

		public override void OnBodyGUI() {

			OnTopGUI();

			serializedObject.Update();

			menuContent = menuContent != null ? menuContent : EditorGUIUtility.IconContent("_Menu");

			foreach (var input in target.DynamicInputs) {

				EditorGUILayout.BeginHorizontal();

				NodeEditorGUILayout.PortField(new GUIContent(input.fieldName), input, serializedObject);

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
							if (target.DynamicInputs.Any(x => x != input && x.fieldName.Equals(newName))) {
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

			addInputContent = addInputContent != null ? addInputContent : new GUIContent(EditorGUIUtility.IconContent("CreateAddNew"));
			addInputContent.text = "Add New Input";

			if (GUILayout.Button(addInputContent)) {

				EditorDrawerUtilities.ShowTypeSelectionPopup(
					type => {
						serializedObject.ApplyModifiedProperties();
						var otherInputCount = target.DynamicInputs.Count(x => x.fieldName.Contains(type.Name));
						var name = $"{type.Name}{(otherInputCount > 0 ? otherInputCount.ToString() : string.Empty)}";
						var newInput = target.AddDynamicInput(type, XNode.Node.ConnectionType.Override, XNode.Node.TypeConstraint.Inherited, name);
						serializedObject.Update();
					}
				);
			}

			var waitForContinueProperty = serializedObject.FindProperty(PrintTextNode.WaitForContinueFieldName);
			waitForContinueProperty.boolValue = GUILayout.Toggle(waitForContinueProperty.boolValue, new GUIContent("Wait for Continue",
				"Whether or not to wait for the player to press 'continue' before moving on to the next node"));

			var clearPreviousTextProperty = serializedObject.FindProperty(PrintTextNode.ClearPreviousTextFieldName);
			clearPreviousTextProperty.boolValue = GUILayout.Toggle(clearPreviousTextProperty.boolValue, new GUIContent("Clear Previous Text",
				"Whether to clear and replace any previously printed text, or simply append to it."));

			var textPrinterPort = target.GetInputPort(PrintTextNode.TextPrinterFieldName);
			NodeEditorGUILayout.PortField(textPrinterPort, serializedObject);

			var textPort = target.GetInputPort(nameof(PrintTextNode.text));
			NodeEditorGUILayout.PortField(textPort, serializedObject);

			var textProperty = serializedObject.FindProperty(nameof(PrintTextNode.text));

			if (!textPort.IsConnected) {

				#region Rich Text Tools

				var toolButtonWidth = Mathf.Min( 55f, GetWidth()/5);
				EditorGUILayout.BeginHorizontal();

				EditorGUILayout.Space(1, true);

				var enableRichTextProperty = serializedObject.FindProperty(PrintTextNode.EnableRichTextFieldName);
				if (GUILayout.Button("R" , GUILayout.Width(toolButtonWidth))) {
					enableRichTextProperty.boolValue = !enableRichTextProperty.boolValue;
				}

				if (GUILayout.Button("B", GUILayout.Width(toolButtonWidth)) && textEditor != null) {
					if (textEditor.SelectedText.TrimStart().StartsWith("<b>") && textEditor.SelectedText.TrimEnd().EndsWith("</b>")) {
						var unboldedText = textEditor.SelectedText.Substring(3, textEditor.SelectedText.Length - 7);
						textEditor.ReplaceSelection(unboldedText);
						for (var jj = 0; jj < unboldedText.Length; jj++) {
							textEditor.SelectLeft();
						}
					}
					else {
						var boldedText = $"<b>{textEditor.SelectedText}</b>";

						textEditor.ReplaceSelection(boldedText);
						for (var jj = 0; jj < boldedText.Length; jj++) {
							textEditor.SelectLeft();
						}
					}
					textProperty.stringValue = textEditor.text;
				}

				if (GUILayout.Button("I", GUILayout.Width(toolButtonWidth)) && textEditor != null) {
					if (textEditor.SelectedText.TrimStart().StartsWith("<i>") && textEditor.SelectedText.TrimEnd().EndsWith("</i>")) {
						var unboldedText = textEditor.SelectedText.Substring(3, textEditor.SelectedText.Length - 7);
						textEditor.ReplaceSelection(unboldedText);
						for (var jj = 0; jj < unboldedText.Length; jj++) {
							textEditor.SelectLeft();
						}
					}
					else {
						var boldedText = $"<i>{textEditor.SelectedText}</i>";

						textEditor.ReplaceSelection(boldedText);
						for (var jj = 0; jj < boldedText.Length; jj++) {
							textEditor.SelectLeft();
						}
					}
					textProperty.stringValue = textEditor.text;
				}

				textColor = EditorGUILayout.ColorField(textColor, GUILayout.Width(50));

				if (GUILayout.Button("C", GUILayout.Width(toolButtonWidth)) && textEditor != null) {

					if (textEditor.SelectedText.TrimStart().StartsWith("<color=") && textEditor.SelectedText.TrimEnd().EndsWith("</color>")) {
						var closeingTagIndex = textEditor.SelectedText.IndexOf('>');
						var unboldedText = textEditor.SelectedText.Substring(closeingTagIndex+1, textEditor.SelectedText.Length - closeingTagIndex - 1 -8);
						textEditor.ReplaceSelection(unboldedText);
						for (var jj = 0; jj < unboldedText.Length; jj++) {
							textEditor.SelectLeft();
						}
					}
					else {
						var boldedText = $"<color=#{ColorUtility.ToHtmlStringRGB(textColor)}>{textEditor.SelectedText}</color>";

						textEditor.ReplaceSelection(boldedText);
						for (var jj = 0; jj < boldedText.Length; jj++) {
							textEditor.SelectLeft();
						}
					}
					textProperty.stringValue = textEditor.text;
				}

				EditorGUILayout.EndHorizontal();
				#endregion

				var heightProperty = GetHeightProperty();


				scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(heightProperty.intValue));

				if (editorTextStyle == null) {
					editorTextStyle = new GUIStyle(EditorStyles.textArea);
					editorTextStyle.wordWrap = true;
					editorTextStyle.padding.left = 8;
					editorTextStyle.padding.top = 6;
					editorTextStyle.fontSize = 16;
				}

				editorTextStyle.richText = enableRichTextProperty.boolValue;
				GUI.SetNextControlName("text");
				textProperty.stringValue = EditorGUILayout.TextArea(textProperty.stringValue, editorTextStyle, GUILayout.ExpandHeight(true));

				var focusedControl = GUI.GetNameOfFocusedControl();
				if (focusedControl.Equals("text")) {
					var textEditor = typeof(EditorGUI)
						.GetField("activeEditor", BindingFlags.Static | BindingFlags.NonPublic)
						.GetValue(null) as TextEditor;
					if (textEditor != null) {
						this.textEditor = textEditor;
					}
				}



				EditorGUILayout.EndScrollView();

			}

			DrawResizableButton();

			serializedObject.ApplyModifiedProperties();
		}

		protected virtual SerializedProperty GetWidthProperty() {
			var name = PrintTextNode.WidthFieldName;
			return serializedObject.FindProperty(name);
		}

		protected virtual SerializedProperty GetHeightProperty() {
			var name = PrintTextNode.HeightFieldName;
			return serializedObject.FindProperty(name);
		}

		public void DrawResizableButton() {

			var selected = Selection.objects.Contains(target);

			if (selected) {
				var lastRect = GUILayoutUtility.GetLastRect();
				var buttonSize = 30f;
				var rect = new Rect(lastRect.x + lastRect.width - 10, lastRect.y + lastRect.height - 10, buttonSize, buttonSize);

				if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition)) {
					Vector2 pos = NodeEditorWindow.current.WindowToGridPosition(Event.current.mousePosition);
					lastDragX = pos.x;
					lastDragY = pos.y;
					var heightProperty = GetHeightProperty();
					startingHeight = heightProperty.intValue;

					startingWidth = GetWidth();

					dragging = true;
				}

				resizeIcon = resizeIcon != null ? resizeIcon : EditorGUIUtility.IconContent("d_Grid.MoveTool").image as Texture2D;
				GUI.DrawTexture(rect, resizeIcon);

				if (Event.current.type == EventType.MouseUp || Event.current.type == EventType.MouseLeaveWindow) {
					dragging = false;
				}

				if (Event.current.type == EventType.MouseDrag && dragging) {
					Vector2 pos = NodeEditorWindow.current.WindowToGridPosition(Event.current.mousePosition);

					var differenceX = lastDragX - pos.x;
					var weightProperty = GetWidthProperty();
					weightProperty.intValue = Mathf.Max(80, startingWidth - (int)differenceX);

					var differenceY = lastDragY - pos.y;

					var heightProperty = GetHeightProperty();
					heightProperty.intValue = Mathf.Max(20, startingHeight - (int)differenceY);
#if ODIN_INSPECTOR
					GUIHelper.RepaintRequested = true;
#endif
				}
			}

			serializedObject.ApplyModifiedProperties();
		}

		public override int GetWidth() {
			var widthProperty = GetWidthProperty();
			return widthProperty.intValue;
		}

	}
}