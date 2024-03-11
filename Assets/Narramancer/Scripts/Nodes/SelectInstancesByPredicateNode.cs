
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {

	[NodeWidth(250)]
	[CreateNodeMenu("Noun/Select Instances")]
	public class SelectInstancesByPredicateNode : Node {

		[SerializeField]
		[Input(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Strict)]
		private List<NounInstance> inputInstances = default;

		[SerializeField]
		[VerbRequired]
		[HideLabelInNode]
		[RequireInput(typeof(NounInstance), "instance")]
		[RequireOutput(typeof(bool), "result")]
		private ValueVerb predicate = default;

		[SerializeField]
		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Strict)]
		private List<NounInstance> outputInstances = default;

		public override void UpdatePorts() {

			if (predicate == null) {
				ClearDynamicPorts();
			}
			else {
				List<NodePort> existingPorts = new List<NodePort>();

				var instanceInputPort = predicate.GetInput<NounInstance>();

				foreach (var inputGraphPort in predicate.Inputs) {
					if (inputGraphPort.Type == null) {
						continue;
					}
					// don't add an input port for the Character input -> we'll assign that one manually
					if (inputGraphPort == instanceInputPort) {
						continue;
					}
					var nodePort = this.GetOrAddDynamicInput(inputGraphPort.Type, inputGraphPort.Name);
					existingPorts.Add(nodePort);
				}

				this.ClearDynamicPortsExcept(existingPorts);
			}

			base.UpdatePorts();
		}

		private void AssignGraphVariableInputs(INodeContext context) {
			foreach (var inputPort in DynamicInputs) {
				var verbPort = predicate.GetInput(inputPort.ValueType, inputPort.fieldName);
				verbPort.AssignValueFromNodePort(context, inputPort);
			}
		}

		private bool DoesInstancePassPredicate(INodeContext context, NounInstance instance) {
			AssignGraphVariableInputs(context);

			return predicate.RunForValue<bool, NounInstance>(context, instance);
		}

		private List<NounInstance> GetFilteredInstances(INodeContext context) {
			var instances = GetInputValue<List<NounInstance>>(context, nameof(this.inputInstances));
			return instances.Where(instance => DoesInstancePassPredicate(context, instance)).ToList();
		}

		public override object GetValue(INodeContext context, NodePort port) {

			if (Application.isPlaying) {
				if (predicate == null) {
					Debug.LogError($"{this.GetType().Name} requires a predicate assigned", this);
					return null;
				}
				switch (port.fieldName) {
					case nameof(outputInstances):
						return GetFilteredInstances(context);
				}
			}

			return null;
		}
	}
}