
using System.Linq;
using UnityEditor;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CustomNodeEditor(typeof(CommentNode))]
	public class CommentNodeEditor : ResizableNodeEditor {

		private static GUIStyle editorTextStyle;
		private static GUIStyle editorLabelStyle;

		private Vector2 scrollPos;

		public override void OnBodyGUI() {

			serializedObject.Update();

			var heightProperty = GetHeightProperty();
			if (heightProperty.intValue < 0) {
				heightProperty.intValue = 80;
			}

			var commentProperty = serializedObject.FindProperty(nameof(CommentNode.comment));
			scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(heightProperty.intValue));
			var selected = Selection.objects.Contains(target);
			if (selected) {
				if (editorTextStyle == null) {
					editorTextStyle = new GUIStyle(EditorStyles.textArea);
					editorTextStyle.wordWrap = true;
					editorTextStyle.padding.left = 8;
					editorTextStyle.padding.top = 6;
					editorTextStyle.fontSize = 16;
				}
				commentProperty.stringValue = EditorGUILayout.TextArea(commentProperty.stringValue, editorTextStyle, GUILayout.ExpandHeight(true));
			}
			else {
				if (editorLabelStyle == null) {
					editorLabelStyle = new GUIStyle(EditorStyles.label);
					editorLabelStyle.wordWrap = true;
					editorLabelStyle.padding.left = 8;
					editorLabelStyle.padding.top = 6;
					editorLabelStyle.fontSize = 16;
					editorLabelStyle.alignment = TextAnchor.UpperLeft;
				}
				if (GUILayout.Button(commentProperty.stringValue, editorLabelStyle, GUILayout.ExpandHeight(true))) {
					Selection.objects = new[] { target };
				}
			}
			EditorGUILayout.EndScrollView();

			DrawResizableButton(minHeight:20);

			serializedObject.ApplyModifiedProperties();
		}

		public override Color GetTint() {
			var colorProperty = serializedObject.FindProperty(CommentNode.ColorFieldName);
			return colorProperty.colorValue;
		}

	}
}