
using UnityEngine;
using XNode;

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

		[Output(backingValue = ShowBackingValue.Never, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		private float value = 0f;

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

		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying) {
				if (port.fieldName.Equals(nameof(value))) {
					var inputInstance = GetInstance(context);

					var inputStat = GetInputValue(context, nameof(stat), stat);


					var statEffectiveValue = inputInstance.GetStatEffectiveValue(context, inputStat);
					return statEffectiveValue;
				}
			}
			return base.GetValue(context, port);
		}
	}
}