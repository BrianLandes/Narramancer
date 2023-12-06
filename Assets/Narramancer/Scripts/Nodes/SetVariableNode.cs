using UnityEngine;

namespace Narramancer {
	[CreateNodeMenu("Variable/Set Variable")]
	public class SetVariableNode : ChainedRunnableNode {

		[SerializeField]
		SerializableVariableReference variable = new SerializableVariableReference(InputOrOutput.Output);

		public VerbGraph VerbGraph => graph as VerbGraph;

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			var outputNodePort = GetInputPort(SerializableVariableReference.PORT_NAME);
			if (outputNodePort == null) {
				return;
			}
			var inputValue = outputNodePort.GetInputValue(runner.Blackboard);

			var blackboard = variable.GetBlackboard(runner.Blackboard);
			blackboard.Set(variable.Key, inputValue);
		}

		public void SetVariable(SerializableVariableReference.ScopeType scope, NarramancerPort outputPort) {
			variable.SetVariable(scope, outputPort);
			UpdatePorts();
		}


		public override void UpdatePorts() {

			var variable = this.variable.GetVariable(VerbGraph);

			if (variable == null) {
				ClearDynamicPorts();
			}
			else {

				var objectType = variable.Type;

				var outputPort = this.GetOrAddDynamicInput(objectType, SerializableVariableReference.PORT_NAME);

				this.ClearDynamicPortsExcept(outputPort);

			}

			base.UpdatePorts();
		}

	}
}