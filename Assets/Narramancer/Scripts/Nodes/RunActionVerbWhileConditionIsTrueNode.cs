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


		/// <summary>Ids for this node's two continuations. Written into saves verbatim.</summary>
		private const string VerbFinishedCallbackId = "verb-finished";

		private const string PollCallbackId = "poll";

		private string RunningKey => Blackboard.UniqueKey(this, "Running");

		private string SubRunnerKey => Blackboard.UniqueKey(this, "SubRunner");

		private bool IsConditionTrue(NodeRunner runner) {
			return GetInputValue(runner.Blackboard, nameof(this.condition), this.condition);
		}

		private NodeRunner GetSubRunner(NodeRunner runner) {
			return NarramancerSingleton.Instance.GetNodeRunner(runner.Blackboard.GetString(SubRunnerKey));
		}

		/// <summary>
		/// One tick: keep the verb running while the condition holds, stop it when the condition goes false.
		/// Was a self-rescheduling local function, which is what <see cref="OnContinuation"/> exists for.
		/// </summary>
		private void Poll(NodeRunner runner) {
			if (runner == null || !runner.Blackboard.GetBool(RunningKey)) {
				return;
			}

			if (IsConditionTrue(runner)) {
				NarramancerSingleton.Instance
					.MakeTimer(refreshDelay)
					.WhenDone(Continuation.NodeCallbackTo(this, PollCallbackId, runner));
				return;
			}

			runner.Blackboard.Set(RunningKey, false);

			var subRunner = GetSubRunner(runner);
			if (subRunner != null) {
				subRunner.StopAndReset();
				NarramancerSingleton.Instance.ReleaseNodeRunner(subRunner);
			}

			if (TryGetRunnableNodeFromPort(nameof(runNodeOnConditionFalse), out var nextNode)) {
				runner.Prepend(nextNode);
			}

			runner.Resume();
		}

		public override void OnContinuation(string callbackId, NodeRunner runner) {
			if (callbackId == PollCallbackId) {
				Poll(runner);
				return;
			}

			if (callbackId != VerbFinishedCallbackId || runner == null) {
				return;
			}

			// The verb ran to completion before the condition went false.
			if (!runner.Blackboard.GetBool(RunningKey)) {
				return;
			}
			runner.Blackboard.Set(RunningKey, false);

			NarramancerSingleton.Instance.ReleaseNodeRunner(GetSubRunner(runner));

			if (TryGetRunnableNodeFromPort(nameof(runNodeOnTrueAndFinish), out var finishedNode)) {
				runner.Prepend(finishedNode);
			}

			runner.Resume();
		}

		public override void Run(NodeRunner runner) {

			if (IsConditionTrue(runner)) {
				if (actionVerb != null && actionVerb.TryGetFirstRunnableNodeAfterRootNode(out var runnableNode)) {

					runner.Suspend();

					var subRunner = NarramancerSingleton.Instance.CreateSubRunner($"{name} - {graph.name}", runner);
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

					// The verb finishing and the condition going false race each other, and exactly one must
					// take effect. That guard was a bool captured by both closures; it lives on the blackboard
					// now, so a save landing mid-poll carries it.
					runner.Blackboard.Set(RunningKey, true);
					runner.Blackboard.SetString(SubRunnerKey, subRunner.name);

					subRunner.Start(runnableNode).WhenDone(Continuation.NodeCallbackTo(this, VerbFinishedCallbackId, runner));

					Poll(runner);
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

