
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("Flag/Is Flag Raised")]
	public class IsFlagRaisedNode : Node {

		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField, HideLabel]
		private Flag flag = default;

		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
		[SerializeField]
		private bool isRaised = false;

		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying) {
				if (port.fieldName.Equals(nameof(isRaised))) {
					var flag = GetInputValue(context, nameof(this.flag), this.flag);
					return NarramancerSingleton.Instance.IsFlagRaised(flag);
				}
			}
			return null;
		}

	}
}