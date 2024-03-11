
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("Flag/Get Flag")]
	public class GetFlagNode : Node {

		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField, HideLabel]
		private Flag flag = default;

		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
		[SerializeField]
		private int value = 0;

		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying) {
				if (port.fieldName.Equals(nameof(value))) {
					var flag = GetInputValue(context, nameof(this.flag), this.flag);
					return NarramancerSingleton.Instance.GetFlag(flag);
				}
			}
			return null;
		}
	}
}