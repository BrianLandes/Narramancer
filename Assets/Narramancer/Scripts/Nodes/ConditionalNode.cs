using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	[NodeWidth(150)]
	[CreateNodeMenu("Flow/Conditional")]
	public class ConditionalNode : RunnableNode {


		[SerializeField]
		[Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited, backingValue = ShowBackingValue.Unconnected)]
		private bool condition = true;

		[Output(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		public RunnableNode runNodeIfTrue = default;

		[Output(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		public RunnableNode runNodeIfFalse = default;

		public bool IsConditionMet(INodeContext context) {
			return GetInputValue(context, nameof(condition), condition);
		}

		public override void Run(NodeRunner runner) {
			if (IsConditionMet(runner.Blackboard)) {
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