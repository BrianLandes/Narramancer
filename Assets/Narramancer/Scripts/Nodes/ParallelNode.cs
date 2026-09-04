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

				var subRunner = NarramancerSingleton.Instance.CreateSubRunner($"{name} - {node.name}", runner);
				subRunner.Blackboard = runner.Blackboard;

				subRunner.Start(node).WhenDone(() => {
					NarramancerSingleton.Instance.ReleaseNodeRunner(subRunner);
				});

			}
		}

		// No Cancel override: NodeRunner.StopAndReset stops everything it owns, and this node's branches are
		// now owned by the runner that started them. The override this replaced rebuilt each sub-runner's name
		// from three GetHashCode() values and looked it up — after a save/load the name no longer matched, so
		// the lookup returned null and Cancel threw a NullReferenceException.
	}
}