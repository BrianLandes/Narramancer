using System.Collections.Generic;
using System.Linq;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("Verb/Output (Value Verb)")]
	[DisallowMultipleNodes(1)]
	public class OutputNode : Node {

		public void RebuildInputPorts() {

			var ioNodeGraph = graph as VerbGraph;

			if (ioNodeGraph == null) {
				ClearDynamicPorts();
				return;
			}

			List<NodePort> existingPorts = new List<NodePort>();

			foreach (var outputPort in ioNodeGraph.Outputs) {
				if (outputPort.Type == null) {
					continue;
				}
				var nodePort = this.GetOrAddDynamicInput(outputPort.Type, outputPort.Name);
				existingPorts.Add(nodePort);
			}

			foreach (var nodePort in DynamicPorts.Except(existingPorts).ToList()) {
				RemoveDynamicPort(nodePort);
			}

		}



		public object GetValue(INodeContext context, NarramancerPort graphPort) {

			var outputNodePort = GetInputPort(graphPort.Name);
			if (outputNodePort == null) {
				return null;
			}

			var value = outputNodePort.GetInputValue(context);

			return value;
		}

		public override object GetValue(INodeContext context, NodePort port) {
			throw new System.NotImplementedException();
		}
	}
}