using XNodeEditor;

namespace Narramancer {
	[CustomNodeEditor(typeof(TranslateGameObjectNode))]
	public class TranslateGameObjectNodeEditor : ChainedRunnableNodeEditor {

		public override void OnBodyGUI() {

			OnTopGUI();

			serializedObject.Update();

			var gameObjectProperty = serializedObject.FindProperty(TranslateGameObjectNode.GameObjectFieldName);
			NodeEditorGUILayout.PropertyField(gameObjectProperty);

			var targetPositionProperty = serializedObject.FindProperty(TranslateGameObjectNode.TargetPositionFieldName);
			NodeEditorGUILayout.PropertyField(targetPositionProperty);

			var moveTypeProperty = serializedObject.FindProperty(TranslateGameObjectNode.MoveTypeFieldName);
			NodeEditorGUILayout.PropertyField(moveTypeProperty);

			switch ((TranslateGameObjectNode.MoveType)moveTypeProperty.enumValueIndex) {
				case TranslateGameObjectNode.MoveType.Duration:
					var durationProperty = serializedObject.FindProperty(TranslateGameObjectNode.DurationFieldName);
					NodeEditorGUILayout.PropertyField(durationProperty);
					break;
				case TranslateGameObjectNode.MoveType.Speed:
					var moveSpeedProperty = serializedObject.FindProperty(TranslateGameObjectNode.MoveSpeedFieldName);
					NodeEditorGUILayout.PropertyField(moveSpeedProperty);
					break;
				case TranslateGameObjectNode.MoveType.Immediate:
					break;
			}

			if (((TranslateGameObjectNode.MoveType)moveTypeProperty.enumValueIndex) != TranslateGameObjectNode.MoveType.Immediate) {
				var waitProperty = serializedObject.FindProperty(TranslateGameObjectNode.WaitFieldName);
				NodeEditorGUILayout.PropertyField(waitProperty);
			}


			serializedObject.ApplyModifiedProperties();
		}

	}
}