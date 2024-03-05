using UnityEngine;
using XNode;

namespace Narramancer {

	[CreateNodeMenu("Flag/Raise Flag")]
	public class RaiseFlagNode : ChainedRunnableNode {

		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField, HideLabel]
		private Flag flag = default;


		public override void Run(NodeRunner runner) {
			base.Run(runner);
			var flag = GetInputValue(runner.Blackboard, nameof(this.flag), this.flag);
			NarramancerSingleton.Instance.RaiseFlag(flag);
		}

	}
}