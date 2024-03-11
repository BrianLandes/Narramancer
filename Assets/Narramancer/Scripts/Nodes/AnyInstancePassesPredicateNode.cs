
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {

	[NodeWidth(275)]
	[CreateNodeMenu("Noun/Any Instance Passes Predicate")]
	public class AnyInstancePassesPredicateNode : Node {

		[SerializeField]
		[Input(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Strict)]
		private List<NounInstance> instances = default;

		[SerializeField]
		[VerbRequired]
		[RequireInput(typeof(NounInstance))]
		[RequireOutput(typeof(bool))]
		private ValueVerb predicate = default;
		public ValueVerb Predicate => predicate;

		[SerializeField]
		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Strict)]
		private bool result = false;

		[SerializeField]
		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Strict)]
		private NounInstance firstInstance = default;

		public void RebuildPorts() {

			if (predicate == null ) {
				ClearDynamicPorts();
				return;
			}

			var existingPorts = new List<NodePort>();

			var characterInputPort = predicate.GetInput<NounInstance>();

			foreach (var inputGraphPort in predicate.Inputs) {
				if (inputGraphPort.Type == null) {
					continue;
				}
				// don't add an input port for the Character input -> we'll assign that one manually
				if (inputGraphPort == characterInputPort) {
					continue;
				}
				var nodePort = this.GetOrAddDynamicInput(inputGraphPort.Type, inputGraphPort.Name);
				existingPorts.Add(nodePort);
			}

			this.ClearDynamicPortsExcept(existingPorts);
		}

		private bool DoesInstancePassPredicate(INodeContext context, NounInstance instance) {
			if (predicate == null) {
				Debug.LogError($"{nameof(AnyInstancePassesPredicateNode).Nicify()} must have a predicate assigned.", this);
				return false;
			}
			if (predicate.TryGetOutputNode(out var outputNode)) {

				foreach (var inputPort in DynamicInputs) {
					var verbPort = predicate.GetInput(inputPort.ValueType, inputPort.fieldName);
					verbPort.AssignValueFromNodePort(context, inputPort);
				}

				return predicate.RunForValue<bool, NounInstance>(context, instance);

			}
			else {
				Debug.LogError($"{nameof(ValueVerb).Nicify()} does not have an {nameof(OutputNode).Nicify()}.", predicate);
				return false;
			}
		}

		private NounInstance GetFirstInstanceThatPassesPredicate(INodeContext context) {
			var instances = this.GetInputValueList<NounInstance>(context, nameof(this.instances));
			return instances.FirstOrDefault(instance => DoesInstancePassPredicate(context, instance));
		}

		public override object GetValue(INodeContext context, NodePort port) {

			if (Application.isPlaying) {
				switch (port.fieldName) {
					case nameof(result):
						return GetFirstInstanceThatPassesPredicate(context) != null;

					case nameof(firstInstance):
						return GetFirstInstanceThatPassesPredicate(context);
				}
			}

			return null;
		}
	}
}