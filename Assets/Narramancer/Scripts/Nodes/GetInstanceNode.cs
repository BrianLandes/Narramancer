using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("Noun/Get Instance")]
	public class GetInstanceNode : Node {

		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField]
		[HideLabel]
		private NounScriptableObject noun = default;
		public NounScriptableObject Noun { get => noun; set => noun = value; }

		[Output]
		[SerializeField]
		[HideLabel]
		private NounInstance instance = default;

		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying && port.fieldName.Equals(nameof(instance))) {
				var noun = GetInputValue(context, nameof(this.noun), this.noun);
				return NarramancerSingleton.Instance.GetInstance(noun);
			}
			return null;
		}
	}

}
