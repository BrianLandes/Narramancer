using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {

	[NodeWidth(190)]
	[CreateNodeMenu("Flow/Random Next")]
	public class RandomNextNode : RunnableNode {


		[Output(connectionType = ConnectionType.Multiple, typeConstraint = TypeConstraint.Inherited)]
		[SameLine]
		[SerializeField]
		private RunnableNode possibleNodes = default;

		public override void Run(NodeRunner runner) {
			var nextNodes = GetAllNextNodes().ToList();
			if (nextNodes.Count > 0) {
				var nextNode = nextNodes.ChooseOne();
				runner.Prepend(nextNode);
			}
		}

		public IEnumerable<RunnableNode> GetAllNextNodes() {
			var port = GetOutputPort(nameof(possibleNodes));
			return port.GetConnections().Select(nodePort => nodePort.node).Cast<RunnableNode>();
		}

	}

}

