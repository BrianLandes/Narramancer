
using UnityEditor;
using UnityEngine;
using XNodeEditor;
#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
#endif

namespace Narramancer {
	[CustomEditor(typeof(VerbGraph), true)]
#if ODIN_INSPECTOR
	public class VerbGraphInspector : OdinEditor {

		private bool renaming = false;

		public override void OnInspectorGUI() {
			EditorGUI.BeginChangeCheck();

			if (GUILayout.Button("Edit graph", GUILayout.Height(40))) {
				NodeEditorWindow.Open(serializedObject.targetObject as XNode.NodeGraph);
			}

			if (!AssetDatabase.IsMainAsset(serializedObject.targetObject)) {
				EditorDrawerUtilities.RenameField(serializedObject.targetObject, ref renaming);
				
				EditorDrawerUtilities.DuplciateNodeGraphField(serializedObject.targetObject);
			}

			base.OnInspectorGUI();


			if (EditorGUI.EndChangeCheck()) {
				var graph = target as NarramancerGraph;
				graph.ValidatePorts();
			}
		}
	}
#else
	[CanEditMultipleObjects]
	public class VerbGraphInspector : Editor {

		private bool renaming = false;

		public override void OnInspectorGUI() {
			serializedObject.Update();

			EditorGUI.BeginChangeCheck();

			if (GUILayout.Button("Edit graph", GUILayout.Height(40))) {
				NodeEditorWindow.Open(serializedObject.targetObject as XNode.NodeGraph);
			}

			if (!AssetDatabase.IsMainAsset(serializedObject.targetObject)) {
				EditorDrawerUtilities.RenameField(serializedObject.targetObject, ref renaming);

				EditorDrawerUtilities.DuplciateNodeGraphField(serializedObject.targetObject);
			}

			DrawDefaultInspector();

			if (EditorGUI.EndChangeCheck()) {
				var graph = target as VerbGraph;
				graph.ValidatePorts();
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
#endif
}