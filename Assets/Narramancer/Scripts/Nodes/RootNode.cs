using System.Collections.Generic;
using XNode;
namespace Narramancer {
	[NodeWidth(120)]
	[NodeTint("#B99807")]
	[CreateNodeMenu("Verb/Root (Action Verb)")]
	[DisallowMultipleNodes(1)]
	public class RootNode : Node {

		[Output(connectionType = ConnectionType.Override)]
		public RunnableNode runNode;

		public override object GetValue(INodeContext context, NodePort port) {
			
			// the only thing that this node can be hooked to is 'runNode'
			// the 'value' is the node itself

			return GetNextNode();
		}

		/// <summary>
		/// Returns the node itself that is connected to the 'runNode' port (if there is one, null otherwise).
		/// </summary>
		public RunnableNode GetNextNode() {
			var port = GetOutputPort(nameof(runNode));
			if (!port.IsConnected) {
				return null;
			}
			var connections = port.GetConnections();
			if (connections.Count == 0) {
				UnityEngine.Debug.LogError($"{this.name} node has a null node connected to it");
				return null;
			}
			var node = connections[0].node as RunnableNode;
			return node;
		}

		public bool TryGetNextNode(out RunnableNode nextNode) {
			nextNode = GetNextNode();
			return nextNode != null;
		}
	}
}