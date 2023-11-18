using UnityEditor;

namespace Narramancer {

	[CustomNodeEditor(typeof(ModifyStatNode))]
	public class ModifyStatNodeEditor : AbstractInstanceInputChainedRunnableNodeEditor {

		public override void OnBodyGUI() {
			base.OnBodyGUI();

			var operationProperty = serializedObject.FindProperty(ModifyStatNode.OperationFieldName);
			switch ((ModifyStatNode.Operation)operationProperty.enumValueIndex) {
				case ModifyStatNode.Operation.Increase:
					var maxValue = serializedObject.FindProperty(ModifyStatNode.MaxValueFieldName);
					EditorGUILayout.PropertyField(maxValue);
					break;
				case ModifyStatNode.Operation.Decrease:
					var minValue = serializedObject.FindProperty(ModifyStatNode.MinValueFieldName);
					EditorGUILayout.PropertyField(minValue);
					break;
				case ModifyStatNode.Operation.Set:
					// this area left blank intentionally
					break;
			}
		}
	}
}