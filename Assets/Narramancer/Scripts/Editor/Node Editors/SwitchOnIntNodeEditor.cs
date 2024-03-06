
#if ODIN_INSPECTOR
using Sirenix.Utilities.Editor;
#endif
using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Narramancer {
	[CustomNodeEditor(typeof(SwitchOnIntNode))]
	public class SwitchOnIntNodeEditor : NodeEditor {

		public override void OnBodyGUI() {

			var node = target as SwitchOnIntNode;

			serializedObject.Update();

			var thisNodePort = node.GetThisNodePort();
			NodeEditorGUILayout.PortField(thisNodePort, serializedObject);

			var intValueProperty = serializedObject.FindProperty(SwitchOnIntNode.IntFieldName);
			NodeEditorGUILayout.PropertyField(intValueProperty);

			var minProperty = serializedObject.FindProperty(SwitchOnIntNode.MinValueFieldName);
			var maxProperty = serializedObject.FindProperty(SwitchOnIntNode.MaxValueFieldName);

			EditorGUI.BeginChangeCheck();
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField("Values", GUILayout.Width(40));
			EditorGUILayout.PropertyField(minProperty, GUIContent.none);
			EditorGUILayout.PropertyField(maxProperty, GUIContent.none);
			EditorGUILayout.EndHorizontal();

			if (EditorGUI.EndChangeCheck()) {
				serializedObject.ApplyModifiedProperties();
				node.RebuildPorts();
				serializedObject.Update();
			}

			var defaultNodeProperty = serializedObject.FindProperty(SwitchOnIntNode.DefaultNodeFieldName);
			NodeEditorGUILayout.PropertyField(defaultNodeProperty);

			// Iterate through dynamic ports and draw them in the order in which they are serialized
			foreach (XNode.NodePort dynamicPort in target.DynamicPorts) {
				if (NodeEditorGUILayout.IsDynamicPortListPort(dynamicPort))
					continue;
				NodeEditorGUILayout.PortField(dynamicPort, serializedObject);
			}

			serializedObject.ApplyModifiedProperties();


#if ODIN_INSPECTOR
			// Call repaint so that the graph window elements respond properly to layout changes coming from Odin
			if (GUIHelper.RepaintRequested) {
				GUIHelper.ClearRepaintRequest();
				window.Repaint();
			}
#endif

#if ODIN_INSPECTOR
			inNodeEditor = false;
#endif
		}

	}
}