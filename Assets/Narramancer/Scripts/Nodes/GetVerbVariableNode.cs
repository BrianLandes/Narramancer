
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("Variable/Get Verb Variable")]
	public class GetVerbVariableNode : Node {

		[SerializeField]
		private string inputId;

		public static string OUTPUT_PORT_NAME = "value";

		public NarramancerPort GetInputGraphPort() {
			var verbGraph = graph as VerbGraph;
			if (verbGraph == null) {
				return null;
			}
			if (inputId.IsNullOrEmpty()) {
				return null;
			}
			return verbGraph.Inputs.FirstOrDefault(x => x.Id.Equals(inputId));
		}

		public void SetInput( InputNarramancerPort graphPort) {
			inputId = graphPort.Id;
			ApplyChanges();
		}

		public void ApplyChanges() {

			var input = GetInputGraphPort();

			if (input == null) {
				ClearDynamicPorts();
				return;
			}

			var objectType = input.Type;

			var outputPort = this.GetOrAddDynamicOutput(objectType, OUTPUT_PORT_NAME);

			this.ClearDynamicPortsExcept(outputPort);
		}


		public override object GetValue(object context, NodePort port) {
			if (!Application.isPlaying) {
				return null;
			}
			var input = GetInputGraphPort();
			if (input == null) {
				return null;
			}
			var blackboard = context as Blackboard;
			var value = blackboard.Get(input.VariableKey, input.Type);
			return value;
		}
	}
}