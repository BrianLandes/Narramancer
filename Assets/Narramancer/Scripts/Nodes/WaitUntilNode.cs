using UnityEngine;

namespace Narramancer {

	[NodeWidth(200)]
	[CreateNodeMenu("Flow/Wait Until")]
	public class WaitUntilNode : ChainedRunnableNode {

		/// <summary>
		/// The id this node's polling continuation is stored under. It is written into save files verbatim,
		/// so it is part of the save format — but unlike the compiler-generated lambda name it replaces, it
		/// only changes when someone deliberately changes it.
		/// </summary>
		private const string PollCallbackId = "poll";

		[SerializeField]
		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		bool condition = true;

		[SerializeField]
		[Tooltip("In seconds")]
		float refreshDelay = 0.2f;

		private string RunningKey => Blackboard.UniqueKey(this, "Running");

		private bool IsConditionTrue(NodeRunner runner) {
			return GetInputValue(runner.Blackboard, nameof(this.condition), this.condition);
		}

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			if (IsConditionTrue(runner)) {
				return;
			}

			runner.Suspend();
			runner.Blackboard.Set(RunningKey, true);
			Poll(runner);
		}

		/// <summary>
		/// One tick of the wait: resume if the condition has become true, otherwise schedule another tick.
		/// <para>
		/// This was a local function that rescheduled itself through a captured lambda, which is exactly the
		/// shape a data record cannot express — hence <see cref="OnContinuation"/>. The "still running" flag
		/// lives on the runner's blackboard rather than in a captured local for the same reason: a save has to
		/// be able to carry it.
		/// </para>
		/// </summary>
		private void Poll(NodeRunner runner) {
			if (runner == null || !runner.Blackboard.GetBool(RunningKey)) {
				return;
			}

			if (IsConditionTrue(runner)) {
				runner.Resume();
			}
			else {
				NarramancerSingleton.Instance
					.MakeTimer(refreshDelay)
					.WhenDone(Continuation.NodeCallbackTo(this, PollCallbackId, runner));
			}
		}

		public override void OnContinuation(string callbackId, NodeRunner runner) {
			if (callbackId == PollCallbackId) {
				Poll(runner);
			}
		}

		public override void Cancel(NodeRunner runner) {
			runner.Blackboard.Set(RunningKey, false);
		}
	}
}
