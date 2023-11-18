using UnityEditor;
using UnityEngine;

namespace Narramancer {

	[CustomNodeEditor(typeof(OfferChoicesNode))]
	public class OfferChoicesNodeEditor : RunnableNodeEditor {

		public override void OnBodyGUI() {
			base.OnBodyGUI();

			if (GUILayout.Button("Create New Choice")) {

				var targetNode = target as OfferChoicesNode;
				var newNode = targetNode.AddChoiceNode();

				GenericMenu context = new GenericMenu();

				context.AddItem(new GUIContent("Jump to new choice"), false, () => {
					Selection.objects = new[] { newNode };
					window.Home();
				});


				Matrix4x4 originalMatrix = GUI.matrix;
				GUI.matrix = Matrix4x4.identity;
				context.ShowAsContext();
				GUI.matrix = originalMatrix;
			}
		}
	}
}