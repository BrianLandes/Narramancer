

using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("Variable/Set Blackboard Variable")]
	public class SetBlackboardVariableNode : ChainedRunnableNode {


		[SerializeField]
		private SerializableType valueType = new SerializableType();

		[SerializeField]
		string key = "value";

		private const string INPUT_PORT = "Input";
		private const string OUTPUT_PORT = "Output";

		protected override void Init() {
			valueType.OnChanged -= RebuildPorts;
			valueType.OnChanged += RebuildPorts;
		}

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			var nodePort = GetInputPort(INPUT_PORT);
			if (nodePort == null) {
				return;
			}

			var inputValue = nodePort.GetInputValue(runner.Blackboard);
			runner.Blackboard.Set(key, inputValue);

		}

		private void RebuildPorts() {

			if (valueType.Type == null) {
				ClearDynamicPorts();
				return;
			}

			List<NodePort> existingPorts = new List<NodePort>();

			var inputListPort = this.GetOrAddDynamicInput(valueType.Type, INPUT_PORT);
			existingPorts.Add(inputListPort);

			var outputValuePort = this.GetOrAddDynamicOutput(valueType.Type, OUTPUT_PORT, true, true);
			existingPorts.Add(outputValuePort);

			this.ClearDynamicPortsExcept(existingPorts);

		}

		public override object GetValue(INodeContext context, NodePort port) {
			if (!Application.isPlaying) {
				return null;
			}

			if (port.fieldName.Equals(OUTPUT_PORT)) {
				var blackboard = context as Blackboard;
				var value = blackboard.Get(key, port.ValueType);
				return value;
			}
			return null;
		}
	}

}