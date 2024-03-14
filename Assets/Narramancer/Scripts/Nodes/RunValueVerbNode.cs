
using System;
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {

	[NodeWidth(350)]
	[CreateNodeMenu("Verb/Run Value Verb")]
	public class RunValueVerbNode : Node {

		[SerializeField]
		[VerbRequired]
		[HideLabelInNode]
		public ValueVerb valueVerb;

		public override void UpdatePorts() {

			if (valueVerb == null) {
				ClearDynamicPorts();
			}
			else {
				this.BuildIONodeGraphPorts(valueVerb);

				name = valueVerb.name;
				name = "Execute Graph: " + name.Nicify();
			}

			base.UpdatePorts();
		}

		private NarramancerPort GetCorrespondingVerbPort(Type type, string name) {
			return valueVerb.Inputs.FirstOrDefault(x => x.Type == type && x.Name.Equals(name));
		}

		public override object GetValue(INodeContext context, NodePort port) {

			if (Application.isPlaying) {
				if (valueVerb == null) {
					Debug.LogError($"{nameof(ValueVerb).Nicify()} must be assigned.", this);
					return null;
				}
				try {

					if (valueVerb.TryGetOutputNode(out var outputNode)) {

						foreach (var inputPort in DynamicInputs) {
							var verbPort = GetCorrespondingVerbPort(inputPort.ValueType, inputPort.fieldName);
							verbPort.AssignValueFromNodePort(context, inputPort);
						}

						foreach (var outputPort in valueVerb.Outputs) {

							if (port.fieldName.Equals(outputPort.Name)) {

								return outputNode.GetValue(context, outputPort);
							}
						}

						foreach (var inputPort in valueVerb.Inputs) {

							if (inputPort.PassThrough && port.fieldName.StartsWith(inputPort.Name)) {
								var nodePort = DynamicInputs.FirstOrDefault(xnodePort => xnodePort.fieldName.Equals(inputPort.Name));
								if (nodePort != null) {
									return nodePort.GetInputValue(context);
								}
								break;
							}
						}
					}
					else {
						Debug.LogError($"{nameof(ValueVerb).Nicify()} does not have an {nameof(OutputNode).Nicify()}.", valueVerb);
					}
				}
				catch (Exception e) {
					Debug.LogError($"Exception when Getting Value for '{valueVerb.name}' ('{graph.name}'): {e.Message}", valueVerb);
					throw;
				}

			}

			return null;
		}
	}
}

