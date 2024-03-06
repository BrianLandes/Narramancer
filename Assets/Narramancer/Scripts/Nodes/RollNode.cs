using UnityEngine;
using XNode;

namespace Narramancer {
	[NodeWidth(220)]
	[CreateNodeMenu("Flow/Roll")]
	public class RollNode : RunnableNode {


		[SerializeField]
		[Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited, backingValue = ShowBackingValue.Unconnected)]
		[Range(0,1)]
		[Tooltip("Percentage between 0 and 1")]
		private float chance = 0.9f;


		[Output(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		public RunnableNode runNodeIfTrue = default;

		[Output(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		public RunnableNode runNodeIfFalse = default;

		public override void Run(NodeRunner runner) {
			var chanceInput = GetInputValue(runner.Blackboard, nameof(chance), chance);
			if (chanceInput >= 1.0f || Probabilititties.RollCheck(chanceInput)) {
				if (TryGetNextNodeIfTrue(out var nextNode)) {
					runner.Prepend(nextNode);
				}
			}
			else {
				if (TryGetNextNodeIfFalse(out var nextNode)) {
					runner.Prepend(nextNode);
				}
			}
		}

		public RunnableNode GetNextNodeIfTrue() {
			var port = GetOutputPort(nameof(runNodeIfTrue));
			return GetRunnableNodeFromPort(port);
		}

		public bool TryGetNextNodeIfTrue(out RunnableNode runnableNode) {
			runnableNode = GetNextNodeIfTrue();
			return runnableNode != null;
		}

		public RunnableNode GetNextNodeIfFalse() {
			var port = GetOutputPort(nameof(runNodeIfFalse));
			return GetRunnableNodeFromPort(port);
		}

		public bool TryGetNextNodeIfFalse(out RunnableNode runnableNode) {
			runnableNode = GetNextNodeIfFalse();
			return runnableNode != null;
		}

	}
}