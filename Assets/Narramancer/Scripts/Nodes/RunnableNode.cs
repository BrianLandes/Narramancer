
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {
	public abstract class RunnableNode : Node {

		[Input(connectionType = ConnectionType.Multiple,
			backingValue = ShowBackingValue.Never,
			typeConstraint = TypeConstraint.Inherited)]
		[HideInInspector]
		private RunnableNode thisNode = default;
		public static string ThisNodeField => nameof(thisNode);

		public abstract void Run(NodeRunner runner);


		public virtual void Cancel(NodeRunner runner) {
			// This area left blank intentionally
		}

		public override object GetValue(INodeContext context, NodePort port) {

			if (port.fieldName == nameof(thisNode)) {
				// the 'value' is the node itself
				return this;
			}

			if (!port.IsConnected) {
				return null;
			}

			if (port.ValueType.IsAssignableFrom(typeof(RunnableNode))) {
				var connections = port.GetConnections();
				if (connections.Count == 0) {
					//port.IsConnected can equal true and return zero connections if there is a connection to a null node.
					Debug.LogError($"{name} registers as having a connected node, but all results are null.");
					return null;
				}
				// the 'value' is the node itself
				return connections[0].node;
			}

			return null;
		}

		public RunnableNode GetRunnableNodeFromPort(string portName) {
			var port = GetPort(portName);
			return GetRunnableNodeFromPort(port);
		}

		public RunnableNode GetRunnableNodeFromPort(NodePort port) {

			if (port.IsConnected && port.ValueType.IsAssignableFrom(typeof(RunnableNode))) {
				var connections = port.GetConnections();
				if (connections.Count == 0) {
					//port.IsConnected can equal true and return zero connections if there is a connection to a null node.
					Debug.LogError($"{name} registers as having a connected node, but all results are null.");
					return null;
				}
				// the 'value' is the node itself
				var node = connections[0].node as RunnableNode;
				return node;
			}
			return null;
		}

		public bool TryGetRunnableNodeFromPort(string portName, out RunnableNode runnableNode) {
			runnableNode = GetRunnableNodeFromPort(portName);
			return runnableNode != null;
		}

		public bool TryGetRunnableNodeFromPort(NodePort port, out RunnableNode runnableNode) {
			runnableNode = GetRunnableNodeFromPort(port);
			return runnableNode != null;
		}

		public List<RunnableNode> GetConnectedInputRunnableNodes() {
			var thisNodePort = GetInputPort(nameof(thisNode));
			if (thisNodePort.IsConnected && thisNodePort.ValueType.IsAssignableFrom(typeof(RunnableNode))) {
				var connections = thisNodePort.GetConnections();
				return connections.Select(connection => connection.node as RunnableNode).WithoutNulls().ToList();
			}
			return Enumerable.Empty<RunnableNode>().ToList();
		}

		public List<RunnableNode> GetConnectedOutputRunnableNodes() {
			return Outputs.Select(output => GetRunnableNodeFromPort(output)).WithoutNulls().ToList();
		}

		public NodePort GetThisNodePort() {
			return GetInputPort(nameof(thisNode));
		}

		public void ConnectDownstream(RunnableNode runnableNode) {
			var inputPort = runnableNode.GetInputPort(nameof(thisNode));
			var outputPort = Outputs.FirstOrDefault(output => output.ValueType == typeof(RunnableNode));

			if (outputPort != null && !outputPort.IsConnectedTo(inputPort)) {
				outputPort.Connect(inputPort);
			}
		}

		#region Running or Recently Ran Timers

		public float TimeSinceLastRun(NodeRunner runner) {
			if (runner.TryGetLastNodeEvent(this, out var @event)) {
				var difference = Time.time - @event.timeStamp;
				return difference;
			}
			return -1f;
		}

		#endregion

	}
}