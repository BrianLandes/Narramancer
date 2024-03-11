using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("Flow/Switch (Int)")]
	public class SwitchOnIntNode : RunnableNode {

		[SerializeField]
		[Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited, backingValue = ShowBackingValue.Unconnected)]
		private int intValue = 0;
		public static string IntFieldName => nameof(intValue);

		[SerializeField]
		[Output(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Inherited)]
		[Tooltip("The node run when a value outside of the range is reached.")]
		private RunnableNode defaultNode = default;
		public static string DefaultNodeFieldName => nameof(defaultNode);

		[SerializeField]
		private int minValue = 0;
		public static string MinValueFieldName => nameof(minValue);

		[SerializeField]
		private int maxValue = 1;
		public static string MaxValueFieldName => nameof(maxValue);

		protected override void Init() {
			RebuildPorts();
		}

		public override void Run(NodeRunner runner) {
			if (TryGetNextNode(runner.Blackboard, out var nextNode)) {
				runner.Prepend(nextNode);
			}
		}

		/// <summary>
		/// Returns the node itself that is connected to the 'thenRunNode' port (if there is one, null otherwise).
		/// </summary>
		public RunnableNode GetNextNode(INodeContext context) {
			var inputSocket = GetInputValue(context, nameof(intValue), intValue);
			return GetNodeForInt(context, inputSocket);
		}

		private RunnableNode GetNodeForInt(INodeContext context, int value) {
			NodePort port = DynamicOutputs.FirstOrDefault(outputPort => outputPort.fieldName.Equals(value.ToString()));
			if (port != null) {
				return GetValue(context, port) as RunnableNode;
			}
			return GetDefaultNextNode(context);
		}

		private RunnableNode GetDefaultNextNode(INodeContext context) {
			var port = GetOutputPort(nameof(defaultNode));
			return GetValue(context, port) as RunnableNode;
		}

		public bool TryGetNextNode(INodeContext context, out RunnableNode runnableNode) {
			runnableNode = GetNextNode(context);
			return runnableNode != null;
		}

		private IEnumerable<int> GetAllPossibleIntValues() {
			for(int i = minValue; i <= maxValue; i++ ) {
				yield return i;
			}
		}

		public void RebuildPorts() {

			var maintainPorts = new List<NodePort>();

			foreach (var value in GetAllPossibleIntValues()) {

				var nodePort = this.GetOrAddDynamicOutput(typeof(RunnableNode), value.ToString());
				maintainPorts.Add(nodePort);
			}

			this.ClearDynamicPortsExcept(maintainPorts);
		}
	}
}