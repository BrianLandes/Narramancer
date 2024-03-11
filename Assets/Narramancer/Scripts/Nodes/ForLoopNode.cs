
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("Flow/For Loop")]
	public class ForLoopNode : ChainedRunnableNode {

		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField]
		private int startingValue = 0;

		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField]
		private int endingValueInclusive = 1;

		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField]
		private int step = 1;

		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
		[SerializeField]
		private int value = 1;

		private string IndexKey => Blackboard.UniqueKey(this, "index");

		public override void Run(NodeRunner runner) {
			var variableTable = runner.Blackboard;

			var startingValue = GetInputValue(variableTable, nameof(this.startingValue), this.startingValue);
			var index = variableTable.GetInt(IndexKey, startingValue);

			var endingValueInclusive = GetInputValue(variableTable, nameof(this.endingValueInclusive), this.endingValueInclusive);

			if (index > endingValueInclusive) {
				variableTable.RemoveInt(IndexKey);
				// allow the runner to resume / drop out
			}
			else {
				var step = GetInputValue(variableTable, nameof(this.step), this.step);
				if (step == 0) {
					Debug.LogWarning($"Step was zero", this);
				}

				variableTable.SetInt(IndexKey, index + step);

				// 'Push' this same node -> trigger the next iteration
				runner.Prepend(this);

				// 'Push' the next node when processing an element
				if (TryGetNextNode(out var nextNode)) {
					runner.Prepend(nextNode);
				}
			}

		}

		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying ) {
				if ( port.fieldName.Equals(nameof(value))) {
					var blackboard = context as Blackboard;
					return blackboard.GetInt(IndexKey);
				}
			}
			return base.GetValue(context, port);
		}

	}
}