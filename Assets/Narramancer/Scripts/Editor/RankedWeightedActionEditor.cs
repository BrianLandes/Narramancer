using UnityEditor;
using UnityEditor.AnimatedValues;

namespace Narramancer {
	[CustomEditor(typeof(RankedWeightedAction))]
	public class RankedWeightedActionEditor : Editor {

		AnimBool showRankGraphAnimBool;
		AnimBool showWeightGraphAnimBool;

		bool renaming = false;

		private void OnEnable() {
			showRankGraphAnimBool = new AnimBool(false);
			showRankGraphAnimBool.valueChanged.AddListener(Repaint);
			showWeightGraphAnimBool = new AnimBool(false);
			showWeightGraphAnimBool.valueChanged.AddListener(Repaint);
		}

		public override void OnInspectorGUI() {

			serializedObject.Update();

			using (new EditorGUI.DisabledScope(true)) {
				var script = serializedObject.FindProperty("m_Script");
				EditorGUILayout.PropertyField(script, true);
			}

			if (!AssetDatabase.IsMainAsset(serializedObject.targetObject)) {
				EditorDrawerUtilities.RenameField(serializedObject.targetObject, ref renaming);
			}

			var staticRank = serializedObject.FindProperty("staticRank");
			EditorGUILayout.PropertyField(staticRank, true);

			var staticRankActivated = staticRank.FindPropertyRelative("activated");
			showRankGraphAnimBool.target = !staticRankActivated.boolValue;
			if (EditorGUILayout.BeginFadeGroup(showRankGraphAnimBool.faded)) {

				var rankGraph = serializedObject.FindProperty("rankGraph");
				EditorGUILayout.PropertyField(rankGraph, true);

			}
			EditorGUILayout.EndFadeGroup();

			var staticWeight = serializedObject.FindProperty("staticWeight");
			EditorGUILayout.PropertyField(staticWeight, true);

			var staticWeightActivated = staticWeight.FindPropertyRelative("activated");
			showWeightGraphAnimBool.target = !staticWeightActivated.boolValue;
			if (EditorGUILayout.BeginFadeGroup(showWeightGraphAnimBool.faded)) {

				var weightGraph = serializedObject.FindProperty("weightGraph");
				EditorGUILayout.PropertyField(weightGraph, true);

			}
			EditorGUILayout.EndFadeGroup();

			var effectGraph = serializedObject.FindProperty("effectGraph");
			EditorGUILayout.PropertyField(effectGraph, true);

			var references = serializedObject.FindProperty("references");
			EditorGUILayout.PropertyField(references, true);

			serializedObject.ApplyModifiedProperties();
		}
	}
}