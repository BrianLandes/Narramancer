using UnityEngine;
using XNode;

namespace Narramancer {

	[CreateNodeMenu("Adjective/Get Stat")]
	public class GetStatNode : AbstractInstanceInputNode {

		[Input(backingValue = ShowBackingValue.Unconnected, connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		[HideLabel]
		public StatScriptableObject stat = default;

		[Output(backingValue = ShowBackingValue.Never, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		private bool hasStat = false;

		[Output(backingValue = ShowBackingValue.Never, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		private float value = 0f;

		[Output(backingValue = ShowBackingValue.Never, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		private int intValue = 0;

		[Output(backingValue = ShowBackingValue.Never, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		private float percentage = 0f;


		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying) {
				var inputInstance = GetInstance(context);
				if (inputInstance == null) {
					Debug.LogError("Instance was null", this);
					return null;
				}
				var inputStat = GetInputValue(context, nameof(stat), stat);
				if (inputStat == null) {
					Debug.LogError("Stat was null", this);
					return null;
				}
				if (port.fieldName.Equals(nameof(hasStat))) {
					return inputInstance.HasStat(inputStat);
				}
				else
				if (port.fieldName.Equals(nameof(value))) {
					var statEffectiveValue = inputInstance.GetStatEffectiveValue(context, inputStat);
					return statEffectiveValue;
				}
				else
				if (port.fieldName.Equals(nameof(intValue))) {
					var statEffectiveValue = inputInstance.GetStatEffectiveValue(context, inputStat);
					return Mathf.RoundToInt(statEffectiveValue);
				}
				else
				if (port.fieldName.Equals(nameof(percentage))) {
					var percentageValue = inputInstance.GetStatInstance(stat).GetEffectiveValuePercentage(instance, context);
					return percentageValue;
				}
			}
			return base.GetValue(context, port);
		}
	}

}
