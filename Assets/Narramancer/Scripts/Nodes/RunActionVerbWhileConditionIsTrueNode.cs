using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using XNode;

namespace Narramancer {

	[NodeWidth(350)]
	[CreateNodeMenu("Verb/Run Action Verb While Condition")]
	public class RunActionVerbWhileConditionIsTrueNode : RunnableNode {

		[SerializeField]
		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		bool condition = true;

		[SerializeField]
		[Tooltip("In seconds")]
		float refreshDelay = 0.2f;

		[HideLabel]
		[SerializeField]
		public ActionVerb actionVerb;


		[Output(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		public RunnableNode runNodeOnTrueAndFinish;

		[Output(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		public RunnableNode runNodeOnConditionFalse;


		public override void Run(NodeRunner runner) {

			bool IsConditionTrue() {
				var result = GetInputValue(runner.Blackboard, nameof(this.condition), this.condition);
				return result;
			}

			if (IsConditionTrue()) {
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
							Debug.LogError($"Exception during AssignGraphVariableInputs for NodePort '{inputPort.fieldName}', ActionVerb: '{actionVerb.name}', Within Graph: '{graph.name}': {e.Message}");
							throw;
						}
					}

					var subRunnerIsStillRunning = true;

					subRunner.Start(runnableNode).WhenDone(() => {
						subRunnerIsStillRunning = false;

						NarramancerSingleton.Instance.ReleaseNodeRunner(subRunner);

						if (TryGetRunnableNodeFromPort(nameof(runNodeOnTrueAndFinish), out var nextNode)) {
							runner.Prepend(nextNode);
						}

						runner.Resume();
					});

					void RunUpdate() {
						if (!subRunnerIsStillRunning) {
							return;
						}

						if (IsConditionTrue()) {
							NarramancerSingleton.Instance
								.MakeTimer(refreshDelay)
									.WhenDone(() => {
										RunUpdate();
									}
								);
						}
						else {
							subRunner.StopAndReset();
							NarramancerSingleton.Instance.ReleaseNodeRunner(subRunner);

							if (TryGetRunnableNodeFromPort(nameof(runNodeOnConditionFalse), out var nextNode)) {
								runner.Prepend(nextNode);
							}

							runner.Resume();
						}
					}

					RunUpdate();
				}
				else {
					Debug.LogError("Runnable Graph not set or not setup properly.");
				}
			}
			else {

				if (TryGetRunnableNodeFromPort(nameof(runNodeOnConditionFalse), out var nextNode)) {
					runner.Prepend(nextNode);
				}

				// let the master runner continue
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

