
using UnityEngine;

namespace Narramancer {
	[CreateNodeMenu("Adjective/Modify Stat")]
	public class ModifyStatNode : AbstractInstanceInputChainedRunnableNode {

		[SerializeField]
		[Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		private StatScriptableObject stat = default;

		[SerializeField]
		[Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		private float amount = 0f;

		public enum Operation {
			Increase,
			Decrease,
			Set
		}

		[SerializeField]
		[NodeEnum]
		private Operation operation = Operation.Set;
		public static string OperationFieldName => nameof(operation);

		[SerializeField]
		[HideInInspector]
		private ToggleableFloat minValue = new ToggleableFloat(false);
		public static string MinValueFieldName => nameof(minValue);


		[SerializeField]
		[HideInInspector]
		private ToggleableFloat maxValue = new ToggleableFloat(false);
		public static string MaxValueFieldName => nameof(maxValue);

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			var statValue = GetInputValue(runner.Blackboard, nameof(stat), stat);
			if (statValue == null) {
				Debug.LogError("Stat was null", this);
				return;
			}

			var instance = GetInstance(runner.Blackboard);
			if (instance == null) {
				Debug.LogError("instance was null", this);
				return;
			}

			var statInstance = instance.GetStatInstance(statValue);

			var inputAmount = GetInputValue(runner.Blackboard, nameof(amount), amount);

			switch (operation) {
				case Operation.Increase:
					if (maxValue.activated) {
						if (statInstance.Value + inputAmount < maxValue.value) {
							statInstance.Value += inputAmount;
						}
						else
						if (statInstance.Value < maxValue.value) {
							statInstance.Value = maxValue.value;
						}
					}
					else {
						statInstance.Value += inputAmount;
					}
					break;
				case Operation.Decrease:
					if (minValue.activated) {
						if (statInstance.Value - inputAmount > minValue.value) {
							statInstance.Value -= inputAmount;
						}
						else
						if (statInstance.Value > minValue.value) {
							statInstance.Value = minValue.value;
						}
					}
					else {
						statInstance.Value -= inputAmount;
					}
					break;
				case Operation.Set:
					statInstance.Value = inputAmount;
					break;
			}
		}

	}
}