using UnityEngine;
using XNode;

namespace Narramancer {
	public abstract class ChainedRunnableNode : RunnableNode {

		[SameLine]
		[Output(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		[HideInInspector]
		public RunnableNode thenRunNode;
		public static string ThenRunNodeField => nameof(thenRunNode);

		public override void Run(NodeRunner runner) {
			if (TryGetNextNode(out var nextNode)) {
				runner.Prepend(nextNode);
			}
		}

		public NodePort GetNextNodePort() {
			return GetOutputPort(nameof(thenRunNode));
		}

		/// <summary>
		/// Returns the node itself that is connected to the 'thenRunNode' port (if there is one, null otherwise).
		/// </summary>
		public RunnableNode GetNextNode() {
			var port = GetOutputPort(nameof(thenRunNode));

			if (port != null && port.IsConnected && port.ValueType.IsAssignableFrom(typeof(RunnableNode))) {
				var connections = port.GetConnections();
				if (connections.Count == 0) {
					//port.IsConnected can equal true and return zero connections if there is a connection to a null node.
					Debug.LogError($"{name} registers as having a connected node, but all results are null.");
					return null;
				}
				// the 'value' is the node itself
				var value = connections[0].node;
				var runnableNode = value as RunnableNode;
				return runnableNode;
			}
			return null;
		}

		public bool TryGetNextNode(out RunnableNode runnableNode) {
			runnableNode = GetNextNode();
			return runnableNode != null;
		}
	}
}