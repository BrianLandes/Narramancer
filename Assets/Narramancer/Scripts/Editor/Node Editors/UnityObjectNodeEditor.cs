using UnityEditor;
using XNodeEditor;

namespace Narramancer {
	[CustomNodeEditor(typeof(UnityObjectNode))]
	public class UnityObjectNodeEditor : NodeEditor {

		public override void OnBodyGUI() {

			EditorGUI.BeginChangeCheck();

			base.OnBodyGUI();


			if (EditorGUI.EndChangeCheck()) {
				serializedObject.ApplyModifiedProperties();

				target.UpdatePorts();
			}

		}
	}

}