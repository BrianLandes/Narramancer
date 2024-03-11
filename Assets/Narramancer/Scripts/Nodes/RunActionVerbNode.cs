using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;
using XNode;

namespace Narramancer {

	[NodeWidth(350)]
	[CreateNodeMenu("Verb/Run Action Verb")]
	public class RunActionVerbNode : ChainedRunnableNode {

		[SerializeField]
		[VerbRequired]
		[HideLabelInNode]
		[FormerlySerializedAs("runnableGraph")]
		[Input(ShowBackingValue.Unconnected,ConnectionType.Override, TypeConstraint.Inherited)]
		public ActionVerb actionVerb;

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			var actionVerb = GetInputValue(runner.Blackboard, nameof(this.actionVerb), this.actionVerb);

			if (actionVerb != null && actionVerb.TryGetFirstRunnableNodeAfterRootNode(out var runnableNode)) {

				NarramancerPort GetCorrespondingRunnableGraphPort(Type type, string name) {
					return actionVerb.Inputs.FirstOrDefault(x => x.Type == type && x.Name.Equals(name));
				}

				foreach (var inputPort in DynamicInputs) {
					try {
						var runnableGraphPort = GetCorrespondingRunnableGraphPort(inputPort.ValueType, inputPort.fieldName);
						Assert.IsNotNull(runnableGraphPort);
						runnableGraphPort.AssignValueFromNodePort(runner.Blackboard, inputPort);
					}
					catch (Exception e) {
						Debug.LogError($"Exception during AssignGraphVariableInputs for NodePort '{inputPort.fieldName}', RunnableGraph: '{actionVerb.name}', Within Graph: '{graph.name}': {e.Message}", actionVerb);
						throw;
					}
				}

				runner.Prepend(runnableNode);
			}
			else {
				if (actionVerb == null) {
					Debug.LogError("Graph to run must not be null.", this);
				}
				else {
					Debug.LogError("Runnable Graph missing Root Node", actionVerb);
				}

			}
		}

		public override void UpdatePorts() {

			if (actionVerb == null) {
				ClearDynamicPorts();
			}
			else {
				this.BuildIONodeGraphPorts(actionVerb);

				name = actionVerb.name;
				name = "Run ActionVerb: " + name.Nicify();
			}

			base.UpdatePorts();
		}


		public override object GetValue(INodeContext context, NodePort port) {
			var actionVerb = GetInputValue(context, nameof(this.actionVerb), this.actionVerb);
			if (Application.isPlaying && actionVerb != null) {
				foreach (var outputPort in actionVerb.Outputs) {

					if (port.fieldName.Equals(outputPort.Name)) {
						var blackboard = context as Blackboard;
						var value = blackboard.Get(outputPort.VariableKey, outputPort.Type);
						return value;
					}
				}

				foreach (var inputPort in actionVerb.Inputs) {

					if (inputPort.PassThrough && port.fieldName.StartsWith(inputPort.Name)) {
						var nodePort = DynamicInputs.FirstOrDefault(xnodePort => xnodePort.fieldName.Equals(inputPort.Name));
						if (nodePort != null) {
							return nodePort.GetInputValue(context);
						}
						break;
					}
				}
			}

			return base.GetValue(context, port);
		}

	}
}

