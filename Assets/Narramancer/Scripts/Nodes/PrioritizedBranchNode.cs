using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {

	[NodeWidth(250)]
	[CreateNodeMenu("Flow/Prioritized Branch")]
	public class PrioritizedBranchNode : Node {

		[Input(connectionType = ConnectionType.Multiple,
			backingValue = ShowBackingValue.Never,
			typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		PrioritizedBranchNode thisChoice = default;
		public static string ThisChoiceFieldName => nameof(thisChoice);

		[Output(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		[SameLine]
		[HideLabel]
		RunnableNode thenRunNode = default;
		public RunnableNode ThenRunNode => this.GetRunnableNodeFromPort(nameof(thenRunNode));

		[Input(connectionType = ConnectionType.Override,
			backingValue = ShowBackingValue.Unconnected,
			typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		bool condition = true;

		public override object GetValue(INodeContext context, NodePort port) {

			if (port.fieldName == nameof(thisChoice)) {
				// the 'value' is the node itself
				return this;
			}

			if (port.ValueType.IsAssignableFrom(typeof(RunnableNode))) {
				var connections = port.GetConnections();
				if (connections.Count == 0) {
					return null;
				}
				// the 'value' is the node itself
				return connections[0].node;
			}

			return null;
		}

		public bool IsConditionMet(INodeContext context) {
			return GetInputValue(context, nameof(condition), condition);
		}

	}
}