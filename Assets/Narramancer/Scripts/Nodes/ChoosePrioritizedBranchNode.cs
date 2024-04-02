
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {

	[NodeWidth(250)]
	[CreateNodeMenu("Flow/Choose Prioritized Branch")]
	public class ChoosePrioritizedBranchNode : RunnableNode {


		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
		[SerializeField]
		[SameLine]
		PrioritizedBranchNode branches = default;


		[SerializeField]
		[Tooltip("In seconds")]
		float refreshDelay = 0.2f;

		[Output(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		RunnableNode runNodeOnAllFalse = default;

		[Output(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		RunnableNode runNodeOnBranchCompleted = default;

		[Output(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		RunnableNode runNodeOnCancel = default;


		public IOrderedEnumerable<PrioritizedBranchNode> Branches {
			get {
				var port = GetOutputPort(nameof(branches));
				var connections = port.GetConnections();
				if (connections == null) {
					return Enumerable.Empty<PrioritizedBranchNode>().OrderBy(node => node.position.y);
				}
				var connectedNodes = connections.Select(connection => connection.node).ToList();
				var connectedCoiceNodes = connectedNodes.Cast<PrioritizedBranchNode>().ToList();
				return connectedCoiceNodes.OrderBy(choiceNode => choiceNode.position.y);
			}
		}

		private PrioritizedBranchNode ChooseFirstTrueBranch(INodeContext blackboard) {

			foreach (var branch in Branches) {
				if (branch.IsConditionMet(blackboard)) {
					return branch;
				}
			}
			return null;
		}

		private string SubRunnerName(NodeRunner runner) {
			return $"{runner.name} {this.GetHashCode()}";
		}

		public override void Run(NodeRunner runner) {


			var chosenBranch = ChooseFirstTrueBranch(runner.Blackboard);
			var nextNode = chosenBranch?.ThenRunNode;
			if (chosenBranch != null && nextNode != null) {

				runner.Suspend();

				var subRunner = NarramancerSingleton.Instance.CreateNodeRunner(SubRunnerName(runner));
				subRunner.Blackboard = runner.Blackboard;

				var runningKey = Blackboard.UniqueKey(this, "Running");

				runner.Blackboard.Set(runningKey, true);

				subRunner.Start(nextNode).WhenDone(() => {
					runner.Blackboard.Set(runningKey, false);
				});

				RunUpdate();

				void RunUpdate() {
					var subRunnerStillRunning = runner.Blackboard.GetBool(runningKey);
					if (!subRunnerStillRunning) {

						NarramancerSingleton.Instance.ReleaseNodeRunner(subRunner);

						if (TryGetRunnableNodeFromPort(nameof(runNodeOnBranchCompleted), out var node)) {
							runner.Prepend(node);
						}

						runner.Resume();
						return;
					}

					var furtherChosenBranch = ChooseFirstTrueBranch(runner.Blackboard);

					if (furtherChosenBranch != null) {
						if (furtherChosenBranch == chosenBranch) {
							NarramancerSingleton.Instance
							.MakeTimer(refreshDelay)
								.WhenDone(() => {
									RunUpdate();
								}
							);
						}
						else {
							// chosen branches are different -> stop the current runner, run the new one
							subRunner.StopAndReset();
							NarramancerSingleton.Instance.ReleaseNodeRunner(subRunner);


							// not most effecient way, but rerun this node from the top, reperforming conditional calculations but ensuring that the process happens properly
							runner.Prepend(this);

							if (TryGetRunnableNodeFromPort(nameof(runNodeOnCancel), out var node)) {
								runner.Prepend(node);
							}

							runner.Resume();
						}
					}
					else {
						subRunner.StopAndReset();
						NarramancerSingleton.Instance.ReleaseNodeRunner(subRunner);

						if (TryGetRunnableNodeFromPort(nameof(runNodeOnAllFalse), out var node)) {
							runner.Prepend(node);
						}

						runner.Resume();
					}
				}
			}
			else {
				if (TryGetRunnableNodeFromPort(nameof(runNodeOnAllFalse), out var node)) {
					runner.Prepend(node);
				}
			}
		}

		public override void Cancel(NodeRunner runner) {
			var runningKey = Blackboard.UniqueKey(this, "Running");
			var subRunnerStillRunning = runner.Blackboard.GetBool(runningKey);

			if (subRunnerStillRunning) {
				var subRunner = NarramancerSingleton.Instance.GetNodeRunner(SubRunnerName(runner));
				if (subRunner != null) {
					subRunner.StopAndReset();
					NarramancerSingleton.Instance.ReleaseNodeRunner(subRunner);
				}


				runner.Blackboard.Set(runningKey, false);

			}
		}
		public Node AddBranch() {
			var position = this.position + new Vector2(300, 0);
			var lastNode = Branches.LastOrDefault();
			if (lastNode != null) {
				position = lastNode.position + new Vector2(0, 200);
			}

			var newNode = graph.AddNode<PrioritizedBranchNode>(position);
			var choicePort = newNode.GetInputPort(PrioritizedBranchNode.ThisChoiceFieldName);
			var thisPort = GetOutputPort(nameof(branches));
			thisPort.Connect(choicePort);
			return newNode;
		}

	}
}