using UnityEditor;
using UnityEngine;

namespace Narramancer {

	[CustomNodeEditor(typeof(ChoosePrioritizedBranchNode))]
	public class ChoosePrioritizedBranchNodeEditor : RunnableNodeEditor {

		public override void OnBodyGUI() {
			base.OnBodyGUI();

			if (GUILayout.Button("Create New Branch")) {

				var targetNode = target as ChoosePrioritizedBranchNode;
				var newNode = targetNode.AddBranch();

				GenericMenu context = new GenericMenu();

				context.AddItem(new GUIContent("Jump to new branch"), false, () => {
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