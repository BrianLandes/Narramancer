using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("Math/Random Float")]

	public class RandomFloatNode : Node {

		[SerializeField]
		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		float min = 0;

		[SerializeField]
		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		float max = 1;

		[SerializeField]
		bool filtered = true;

		[SerializeField]
		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
		float result;

		public override object GetValue(INodeContext context, NodePort port) {
			var min = GetInputValue(context, nameof(this.min), this.min);

			var max = GetInputValue(context, nameof(this.max), this.max);

			if (filtered) {
				result = Probabilititties.RangeFiltered(min, max);
			}
			else {
				result = Random.Range(min, max);
			}
			
			return result;
		}
	}

}
