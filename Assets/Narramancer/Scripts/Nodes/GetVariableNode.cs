using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("Variable/Get Variable")]
	public class GetVariableNode : Node {


		[SerializeField]
		SerializableVariableReference variable = new SerializableVariableReference(InputOrOutput.Input);
		public SerializableVariableReference.ScopeType Scope => variable.Scope;

		public VerbGraph VerbGraph => graph as VerbGraph;

		public void SetVariable(SerializableVariableReference.ScopeType scope, NarramancerPort outputPort) {
			variable.SetVariable(scope, outputPort);
			UpdatePorts();
		}
		public NarramancerPort GetVariable() {
			return variable.GetVariable(VerbGraph);
		}

		public override void UpdatePorts() {

			var variable = this.variable.GetVariable(VerbGraph);

			if (variable == null) {
				if (Scope != SerializableVariableReference.ScopeType.Scene) {
					ClearDynamicPorts();
				}
			}
			else {

				var objectType = variable.Type;

				var outputPort = this.GetOrAddDynamicOutput(objectType, SerializableVariableReference.PORT_NAME);

				this.ClearDynamicPortsExcept(outputPort);

			}

			base.UpdatePorts();
		}

		public override object GetValue(INodeContext context, NodePort port) {
			if (!Application.isPlaying) {
				return null;
			}
			var variable = this.variable.GetVariable(VerbGraph);
			if (variable == null) {
				return null;
			}
			var blackboard = this.variable.GetBlackboard(context as Blackboard);
			var value = blackboard.Get(variable.VariableKey, variable.Type);
			return value;
		}
	}
}