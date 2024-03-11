using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
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
		private float percentage = 0f;


		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying) {
				if (port.fieldName.Equals(nameof(hasStat))) {
					var inputInstance = GetInstance(context);
					var inputStat = GetInputValue(context, nameof(stat), stat);
					if (inputInstance != null && inputStat != null) {
						return inputInstance.HasStat(inputStat);
					}
				}
				else
				if (port.fieldName.Equals(nameof(value))) {
					var inputInstance = GetInstance(context);
					Assert.IsNotNull(inputInstance);
					var inputStat = GetInputValue(context, nameof(stat), stat);
					Assert.IsNotNull(inputStat);

					var statEffectiveValue = inputInstance.GetStatEffectiveValue(context, inputStat);
					return statEffectiveValue;
				}
				else
				if (port.fieldName.Equals(nameof(percentage))) {
					var inputInstance = GetInstance(context);
					Assert.IsNotNull(inputInstance);
					var inputStat = GetInputValue(context, nameof(stat), stat);
					Assert.IsNotNull(inputStat);

					var percentageValue = inputInstance.GetStatInstance(stat).GetEffectiveValuePercentage(instance, context);
					return percentageValue;
				}
			}
			return base.GetValue(context, port);
		}
	}

}
