using UnityEngine;
using XNode;

namespace Narramancer {

	[CreateNodeMenu("Flag/Set Flag")]
	public class SetFlagNode : ChainedRunnableNode {

		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField, HideLabel]
		private Flag flag = default;

		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField]
		private int value = 1;


		public override void Run(NodeRunner runner) {
			base.Run(runner);
			var flag = GetInputValue(runner.Blackboard, nameof(this.flag), this.flag);
			var value = GetInputValue(runner.Blackboard, nameof(this.value), this.value);
			NarramancerSingleton.Instance.SetFlag(flag, value);
		}
	}
}