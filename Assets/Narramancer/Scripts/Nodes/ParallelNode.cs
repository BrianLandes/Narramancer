using System;
using System.Collections.Generic;
using System.Linq;
using XNode;

namespace Narramancer {

	[NodeWidth(160)]
	[CreateNodeMenu("Flow/Parallel")]
	public class ParallelNode : RunnableNode {

		[Output(connectionType = ConnectionType.Multiple, typeConstraint = TypeConstraint.Inherited)]
		[SameLine]
		public RunnableNode nodes;

		/// <summary>
		/// Returns the node itself that is connected to the 'thenRunNode' port (if there is one, null otherwise).
		/// </summary>
		public IEnumerable<RunnableNode> GetNextNodes() {
			var port = GetOutputPort(nameof(this.nodes));
			var nodes = port.GetConnections()
				.Select(x => x.node)
				.Cast<RunnableNode>()
				.OrderBy(node => node.position.y);
			return nodes.ToList();
		}

		public override void Run(NodeRunner runner) {

			var nodes = GetNextNodes();

			if (!nodes.Any() ) {
				return;
			}

			runner.Suspend();

			foreach (var node in nodes.Reverse()) {

				var subRunnerName = SubRunnerName(node, runner);
				var subRunner = NarramancerSingleton.Instance.CreateNodeRunner(subRunnerName);
				subRunner.Blackboard = runner.Blackboard;

				subRunner.Start(node).WhenDone(() => {
					NarramancerSingleton.Instance.ReleaseNodeRunner(subRunner);
				});

			}
		}

		private string NameKey(RunnableNode node, NodeRunner runner) {
			return $"Name {runner.GetHashCode()} {this.GetHashCode()} {node.GetHashCode()}";
		}

		private string SubRunnerName(RunnableNode node, NodeRunner runner) {
			var nameKey = NameKey(node, runner);
			return $"{name} - {graph.name} ({nameKey})";
		}

		public override void Cancel(NodeRunner runner) {

			var nodes = GetNextNodes();

			if (!nodes.Any()) {
				return;
			}

			foreach (var node in nodes.Reverse()) {

				var subRunnerName = SubRunnerName(node, runner);
				var subRunner = NarramancerSingleton.Instance.GetNodeRunner(subRunnerName);
				subRunner.StopAndReset();
				NarramancerSingleton.Instance.ReleaseNodeRunner(subRunner);

			}
		}
	}
}