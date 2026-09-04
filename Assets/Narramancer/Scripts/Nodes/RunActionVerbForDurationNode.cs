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

		/// <summary>Ids for this node's two continuations. Written into saves verbatim.</summary>
		private const string VerbFinishedCallbackId = "verb-finished";

		private const string TimeoutCallbackId = "timeout";

		private string RunningKey => Blackboard.UniqueKey(this, "Running");

		private string SubRunnerKey => Blackboard.UniqueKey(this, "SubRunner");

		public override void OnContinuation(string callbackId, NodeRunner runner) {
			if (runner == null) {
				return;
			}

			// Whichever of the two arrives first wins, and clearing the flag makes the other a no-op.
			if (!runner.Blackboard.GetBool(RunningKey)) {
				return;
			}
			runner.Blackboard.Set(RunningKey, false);

			if (callbackId == TimeoutCallbackId) {
				// Out of time: stop the verb where it is. On the other path it finished on its own.
				var subRunner = NarramancerSingleton.Instance.GetNodeRunner(runner.Blackboard.GetString(SubRunnerKey));
				if (subRunner != null) {
					subRunner.StopAndReset();
				}
			}

			NarramancerSingleton.Instance.ReleaseNodeRunner(
				NarramancerSingleton.Instance.GetNodeRunner(runner.Blackboard.GetString(SubRunnerKey)));

			if (TryGetRunnableNodeFromPort(nameof(thenRunNode), out var nextNode)) {
				runner.Prepend(nextNode);
			}

			runner.Resume();
		}

		public override void Run(NodeRunner runner) {

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
						Debug.LogError($"Exception during AssignGraphVariableInputs for NodePort '{inputPort.fieldName}', RunnableGraph: '{actionVerb.name}', Within Graph: '{graph.name}': {e.Message}");
						throw;
					}
				}

				// The verb finishing and the duration elapsing race each other, and exactly one of them must
				// take effect. That guard used to be a bool captured by both lambdas; it lives on the
				// blackboard now, because a save can land between the two.
				runner.Blackboard.Set(RunningKey, true);
				runner.Blackboard.SetString(SubRunnerKey, subRunner.name);

				subRunner.Start(runnableNode).WhenDone(Continuation.NodeCallbackTo(this, VerbFinishedCallbackId, runner));

				var duration = GetInputValue(runner.Blackboard, nameof(this.duration), this.duration);

				NarramancerSingleton.Instance
					.MakeTimer(duration)
					.WhenDone(Continuation.NodeCallbackTo(this, TimeoutCallbackId, runner));
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

