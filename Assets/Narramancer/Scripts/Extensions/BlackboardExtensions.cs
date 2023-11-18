using UnityEngine;
using XNode;

namespace Narramancer {
	public static class BlackboardExtensions {

		public static RunnableNode GetRunnableNodeFromPort(this Node node, string portName) {
			var port = node.GetPort(portName);
			return GetRunnableNodeFromPort(node, port);
		}

		public static RunnableNode GetRunnableNodeFromPort(this Node node, NodePort port) {

			if (port.IsConnected && port.ValueType.IsAssignableFrom(typeof(RunnableNode))) {
				var connections = port.GetConnections();
				if (connections.Count == 0) {
					//port.IsConnected can equal true and return zero connections if there is a connection to a null node.
					Debug.LogError($"{node.name} registers as having a connected node, but all results are null.");
					return null;
				}
				// the 'value' is the node itself
				var runnableNode = connections[0].node as RunnableNode;
				return runnableNode;
			}
			return null;
		}

	}
}