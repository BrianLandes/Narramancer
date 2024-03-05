using UnityEngine;
using XNode;

namespace Narramancer {
	public class DebugLogNode : ChainedRunnableNode {

		[Input(ShowBackingValue.Unconnected, ConnectionType.Override)]
		[TextArea(1, 6), HideLabel]
		public string message;

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			var messageString = GetInputValue(runner.Blackboard, nameof(message), message);

			Debug.Log(messageString);
		}
	}
}