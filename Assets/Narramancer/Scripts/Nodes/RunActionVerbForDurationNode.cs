using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using XNode;

namespace Narramancer {

	[NodeWidth(350)]
	[CreateNodeMenu("Verb/Run Action Verb for Duration")]
	public class RunActionVerbForDurationNode : RunnableNode {

		[SerializeField]
		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		float duration = 6;

		[HideLabel]
		[SerializeField]
		public ActionVerb actionVerb;


		[Output(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		public RunnableNode thenRunNode;

		public override void Run(NodeRunner runner) {

			if (actionVerb != null && actionVerb.TryGetFirstRunnableNodeAfterRootNode(out var runnableNode)) {

				runner.Suspend();

				var subRunner = NarramancerSingleton.Instance.CreateNodeRunner($"{name} - {graph.name}");
				subRunner.Blackboard = runner.Blackboard;

				foreach (var inputPort in DynamicInputs) {
					try {
						var verbPort = GetCorrespondingVerbPort(inputPort.ValueType, inputPort.fieldName);
						Assert.IsNotNull(verbPort);
						verbPort.AssignValueFromNodePort(runner.Blackboard, inputPort);
					}
					catch (Exception e) {
						Debug.LogError($"Exception during AssignGraphVariableInputs for NodePort '{inputPort.fieldName}', RunnableGraph: '{actionVerb.name}', Within Graph: '{graph.name}': {e.Message}");
						throw;
					}
				}

				var subRunnerStillRunning = true;

				subRunner.Start(runnableNode).WhenDone(() => {
					subRunnerStillRunning = false;

					NarramancerSingleton.Instance.ReleaseNodeRunner(subRunner);

					if (TryGetRunnableNodeFromPort(nameof(thenRunNode), out var nextNode)) {
						runner.Prepend(nextNode);
					}

					runner.Resume();
				});

				var duration = GetInputValue(runner.Blackboard, nameof(this.duration), this.duration);

				NarramancerSingleton.Instance
					.MakeTimer(duration)
						.WhenDone(() => {

							if (!subRunnerStillRunning) {
								return;
							}

							NarramancerSingleton.Instance.ReleaseNodeRunner(subRunner);

							if (TryGetRunnableNodeFromPort(nameof(thenRunNode), out var nextNode)) {
								runner.Prepend(nextNode);
							}

							runner.Resume();
						}
					);
			}
			else {
				Debug.LogError("Runnable Graph not set or not setup properly.");
			}
		}

		public override void UpdatePorts() {

			if (actionVerb == null) {
				ClearDynamicPorts();
			}
			else {

				this.BuildIONodeGraphPorts(actionVerb);

				name = actionVerb.name;
				name = "Run Graph: " + name.Nicify();
			}

			base.UpdatePorts();
		}


		private NarramancerPort GetCorrespondingVerbPort(Type type, string name) {
			return actionVerb.Inputs.FirstOrDefault(x => x.Type == type && x.Name.Equals(name));
		}

		public override object GetValue(INodeContext context, NodePort port) {

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

