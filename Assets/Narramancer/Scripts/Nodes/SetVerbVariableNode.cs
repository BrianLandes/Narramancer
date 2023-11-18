
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("Variable/Set Verb Variable")]
	public class SetVerbVariableNode : ChainedRunnableNode {

		[SerializeField]
		private string outputId;

		public static string PORT_NAME = "value";

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			var outputPort = GetOutputPort();

			if (outputPort == null) {
				return;
			}

			var outputNodePort = GetInputPort(PORT_NAME);
			if (outputNodePort == null) {
				return;
			}

			outputPort.AssignValueFromNodePort(runner.Blackboard, outputNodePort);

		}

		private NarramancerPort GetOutputPort() {
			var verbGraph = graph as VerbGraph;
			if (verbGraph == null) {
				return null;
			}
			if (outputId.IsNullOrEmpty()) {
				return null;
			}
			return verbGraph.Outputs.FirstOrDefault(x => x.Id.Equals(outputId));


		}

		public void SetOutput(NarramancerPort outputPort) {
			outputId = outputPort.Id;
			ApplyChanges();
		}

		public void ApplyChanges() {

			var output = GetOutputPort();

			if (output == null) {
				ClearDynamicPorts();
				return;
			}

			var nodePorts = new List<NodePort>();

			var objectType = output.Type;

			var outputPort = this.GetOrAddDynamicInput(objectType, PORT_NAME);
			nodePorts.Add(outputPort);

			this.ClearDynamicPortsExcept(nodePorts);

		}

	}
}