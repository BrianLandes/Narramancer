using UnityEngine;

namespace Narramancer {
	[CreateNodeMenu("Flow/Wait")]
	public class WaitNode : ChainedRunnableNode {

		[SerializeField]
		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		float duration = 1f;

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			runner.Suspend();

			var duration = GetInputValue(runner.Blackboard, nameof(this.duration), this.duration);

			NarramancerSingleton.Instance
				.MakeTimer(duration)
				.WhenDone(() => {
					runner.Resume();
				}
			);
		}
	}

}
