using System;
using UnityEditor;
using UnityEngine;

namespace Narramancer {

	public class EnterTextModalWindow : EditorWindow {

		string text = string.Empty;
		bool autoFocused = false;
		Action<string> onSelect;

		public static void Show(Vector2 position, string placeholderText, Action<string> onTextSelected ) {
			var newWindow = CreateInstance(typeof(EnterTextModalWindow)) as EnterTextModalWindow;
			newWindow.position = new Rect(position.x, position.y, 500, EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing*2 + 20*2);
			newWindow.text = placeholderText;
			newWindow.onSelect = onTextSelected;
			newWindow.ShowPopup();
		}

		void OnGUI() {

			GUILayout.Space(20);

			GUI.SetNextControlName(nameof(text));
			text = EditorGUILayout.TextField(text);
			if (!autoFocused) {
				EditorGUI.FocusTextInControl(nameof(text));
				autoFocused = true;
			}

			if (GUILayout.Button("Confirm") || (Event.current.type == EventType.KeyUp && Event.current.keyCode == KeyCode.Return)) {
				text = text.Trim();
				onSelect?.Invoke(text);
				Close();
			}

			GUILayout.Space(20);

		}

		void OnInspectorUpdate() {
			Repaint();
		}

		private void OnLostFocus() {
			Close();
		}
	}

}