using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {

	[NodeWidth(200)]
	public class StartTimerNode : ChainedRunnableNode {

		[SerializeField]
		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		[Tooltip("In seconds")]
		float duration = 6f;

		[SerializeField]
		[Output(ShowBackingValue.Never, ConnectionType.Multiple)]
		bool timerRunning = false;

		[SerializeField]
		[Output(ShowBackingValue.Never, ConnectionType.Multiple)]
		bool durationPassed = false;

		public override void Run(NodeRunner runner) {
			base.Run(runner);


			var timerKey = Blackboard.UniqueKey(this, "Timer");
			runner.Blackboard.Set(timerKey, Time.time);

		}

		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying) {
				var timerKey = Blackboard.UniqueKey(this, "Timer");
				var blackboard = context as Blackboard;
				var startTime = blackboard.GetFloat(timerKey);
				var elapsed = Time.time - startTime;
				var duration = GetInputValue(context, nameof(this.duration), this.duration);

				if (port.fieldName.Equals(nameof(timerRunning))) {
					return elapsed <= duration;
				}
				else

				if (port.fieldName.Equals(nameof(durationPassed))) {
					return elapsed > duration;
				}
			}
			return base.GetValue(context, port);
		}
	}
}

