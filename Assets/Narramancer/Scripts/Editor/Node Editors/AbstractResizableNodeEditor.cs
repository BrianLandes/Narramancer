#if ODIN_INSPECTOR
using Sirenix.Utilities.Editor;
#endif
using System.Linq;
using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Narramancer {
	public abstract class AbstractResizableNodeEditor : NodeEditor {

		private static Texture2D resizeIcon;

		private bool dragging = false;
		private float lastDragX = 0f;
		private float lastDragY = 0f;
		private int startingWidth = 0;
		private int startingHeight = 0;

		protected abstract string GetWidthPropertyName();
		protected abstract string GetHeightPropertyName();

		protected virtual SerializedProperty GetWidthProperty() {
			var name = GetWidthPropertyName();
			return serializedObject.FindProperty(name);
		}

		protected virtual SerializedProperty GetHeightProperty() {
			var name = GetHeightPropertyName();
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

				resizeIcon = resizeIcon !=null ? resizeIcon : EditorGUIUtility.IconContent("d_Grid.MoveTool").image as Texture2D;
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