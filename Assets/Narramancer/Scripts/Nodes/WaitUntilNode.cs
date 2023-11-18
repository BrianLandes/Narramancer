using UnityEngine;

namespace Narramancer {

	[NodeWidth(200)]
	[CreateNodeMenu("Flow/Wait Until")]
	public class WaitUntilNode : ChainedRunnableNode {

		[SerializeField]
		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		bool condition = true;

		[SerializeField]
		[Tooltip("In seconds")]
		float refreshDelay = 0.2f;

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			bool IsConditionTrue() {
				var result = GetInputValue(runner.Blackboard, nameof(this.condition), this.condition);
				return result;
			}

			if (!IsConditionTrue()) {

				runner.Suspend();

				var runningKey = Blackboard.UniqueKey(this, "Running");
				runner.Blackboard.Set(runningKey, true);

				void RunUpdate() {

					var stillRunning = runner.Blackboard.GetBool(runningKey);

					if (!stillRunning) {
						return;
					}

					if (IsConditionTrue()) {
						runner.Resume();
					}
					else {
						NarramancerSingleton.Instance
							.MakeTimer(refreshDelay)
								.WhenDone(() => {
									RunUpdate();
								}
							);
					}
				}

				RunUpdate();
			}
		}

		public override void Cancel(NodeRunner runner) {

			var runningKey = Blackboard.UniqueKey(this, "Running");
			runner.Blackboard.Set(runningKey, false);
		}
	}
}

