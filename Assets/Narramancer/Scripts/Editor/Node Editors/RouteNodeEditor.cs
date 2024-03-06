using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Narramancer {
	[CustomNodeEditor(typeof(RouteNode))]
	public class RouteNodeEditor : NodeEditor {

		public override void OnBodyGUI() {

			var routeNode = target as RouteNode;


			var thisNodePort = routeNode.GetThisNodePort();
			NodeEditorGUILayout.PortField(new GUIContent("---->"), thisNodePort, serializedObject);

			EditorGUILayout.Space(-EditorGUIUtility.singleLineHeight - EditorGUIUtility.standardVerticalSpacing);

			var nextNodePort = routeNode.GetOutputPort(nameof(RouteNode.thenRunNode));
			NodeEditorGUILayout.PortField(GUIContent.none, nextNodePort, serializedObject);
		}

	}
}