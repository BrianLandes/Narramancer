
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

				EditorDrawerUtilities.ExtractChildNodeGraphField(serializedObject.targetObject);
			}

			base.OnInspectorGUI();


			if (EditorGUI.EndChangeCheck()) {
				var graph = target as NarramancerGraph;
				graph.ValidatePorts();
			}
			
			var inputsProperty = serializedObject.FindProperty(VerbGraph.InputsFieldName);
			for (int ii = 0; ii < inputsProperty.arraySize; ii++) {
				var inputProperty = inputsProperty.GetArrayElementAtIndex(ii);
				var typeProperty = inputProperty.FindPropertyRelative(NarramancerPort.TypeFieldName);
				var canBeListProperty = typeProperty.FindPropertyRelative(nameof(SerializableType.canBeList));
				canBeListProperty.boolValue = true;
			}


			var outputsProperty = serializedObject.FindProperty(VerbGraph.OutputsFieldName);
			for (int ii = 0; ii < outputsProperty.arraySize; ii++) {
				var outputProperty = outputsProperty.GetArrayElementAtIndex(ii);
				var typeProperty = outputProperty.FindPropertyRelative(NarramancerPort.TypeFieldName);
				var canBeListProperty = typeProperty.FindPropertyRelative(nameof(SerializableType.canBeList));
				canBeListProperty.boolValue = true;
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

				EditorDrawerUtilities.DuplicateNodeGraphField(serializedObject.targetObject);

				EditorDrawerUtilities.ExtractChildNodeGraphField(serializedObject.targetObject);
			}

			DrawDefaultInspector();

			if (EditorGUI.EndChangeCheck()) {
				var graph = target as VerbGraph;
				graph.ValidatePorts();
			}


			var inputsProperty = serializedObject.FindProperty(VerbGraph.InputsFieldName);
			for (int ii = 0; ii < inputsProperty.arraySize; ii++) {
				var inputProperty = inputsProperty.GetArrayElementAtIndex(ii);
				var typeProperty = inputProperty.FindPropertyRelative(NarramancerPort.TypeFieldName);
				var canBeListProperty = typeProperty.FindPropertyRelative(nameof(SerializableType.canBeList));
				canBeListProperty.boolValue = true;
			}


			var outputsProperty = serializedObject.FindProperty(VerbGraph.OutputsFieldName);
			for (int ii = 0; ii < outputsProperty.arraySize; ii++) {
				var outputProperty = outputsProperty.GetArrayElementAtIndex(ii);
				var typeProperty = outputProperty.FindPropertyRelative(NarramancerPort.TypeFieldName);
				var canBeListProperty = typeProperty.FindPropertyRelative(nameof(SerializableType.canBeList));
				canBeListProperty.boolValue = true;
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
#endif
}