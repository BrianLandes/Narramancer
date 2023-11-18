
using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Narramancer {

	[CustomNodeEditor(typeof(ChainedRunnableNode))]
	public class ChainedRunnableNodeEditor : RunnableNodeEditor {

		public override void OnTopGUI() {
			base.OnTopGUI();

			EditorGUILayout.Space(-EditorGUIUtility.singleLineHeight - EditorGUIUtility.standardVerticalSpacing);

			var runPort = target.GetOutputPort(ChainedRunnableNode.ThenRunNodeField);
			runPort.UseTriangleHandle = true;
			NodeEditorGUILayout.PortField(runPort, serializedObject);
		}
	}
}