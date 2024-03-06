using System;
using System.Collections.Generic;
using System.Linq;
using XNode;

namespace Narramancer {
	[NodeWidth(160)]
	[CreateNodeMenu("Flow/Sequence")]
	public class SequenceOfNodesNode : RunnableNode {

		[Output(connectionType = ConnectionType.Multiple, typeConstraint = TypeConstraint.Inherited)]
		[SameLine]
		public RunnableNode nodes;

		public override void Run(NodeRunner runner) {
			foreach (var node in GetNextNodes().Reverse()) {
				runner.Prepend(node);
			}
		}

		/// <summary>
		/// Returns the node itself that is connected to the 'thenRunNode' port (if there is one, null otherwise).
		/// </summary>
		public IEnumerable<RunnableNode> GetNextNodes() {
			var port = GetOutputPort(nameof(SequenceOfNodesNode.nodes));
			var nodes = port.GetConnections()
				.Select(x => x.node)
				.Cast<RunnableNode>()
				.OrderBy(node => node.position.y);
			return nodes.ToList();
		}

	}
}