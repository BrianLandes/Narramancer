
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

		// A label only. Nothing looks the sub-runner up by this name any more — the runner that started it
		// holds a reference, so the name no longer has to be reconstructible (and this one was not: it was
		// built from a hash code, which changes when the build does).
		private string SubRunnerName(NodeRunner runner) {
			return $"{runner.name} - {name}";
		}

		/// <summary>
		/// The id this node's polling continuation is stored under. Written into saves verbatim, so treat it
		/// as part of the save format.
		/// </summary>
		private const string PollCallbackId = "poll";

		private string RunningKey => Blackboard.UniqueKey(this, "Running");

		/// <summary>
		/// The three pieces of state the polling loop used to hold in captured locals. They live on the
		/// runner's blackboard now, because a captured local cannot survive a save — and because a save can
		/// arrive in the middle of a poll.
		/// </summary>
		private string SubRunnerKey => Blackboard.UniqueKey(this, "SubRunner");

		private string ChosenBranchKey => Blackboard.UniqueKey(this, "ChosenBranch");

		private PrioritizedBranchNode BranchAt(int index) {
			var branches = Branches.ToList();
			return index >= 0 && index < branches.Count ? branches[index] : null;
		}

		public override void Run(NodeRunner runner) {

			var chosenBranch = ChooseFirstTrueBranch(runner.Blackboard);
			var nextNode = chosenBranch?.ThenRunNode;
			if (chosenBranch != null && nextNode != null) {

				runner.Suspend();

				var subRunner = NarramancerSingleton.Instance.CreateSubRunner(SubRunnerName(runner), runner);
				subRunner.Blackboard = runner.Blackboard;

				runner.Blackboard.Set(RunningKey, true);
				// The runner's real name, not the one asked for: CreateSubRunner disambiguates duplicates.
				runner.Blackboard.SetString(SubRunnerKey, subRunner.name);
				runner.Blackboard.SetInt(ChosenBranchKey, Branches.ToList().IndexOf(chosenBranch));

				subRunner.Start(nextNode).WhenDone(Continuation.SetBool(runner, RunningKey, false));

				Poll(runner);
			}
			else {
				if (TryGetRunnableNodeFromPort(nameof(runNodeOnAllFalse), out var node)) {
					runner.Prepend(node);
				}
			}
		}

		/// <summary>
		/// One tick: finish if the branch completed, keep waiting while the same branch stays the best choice,
		/// and switch branches if a higher-priority one becomes true.
		/// <para>
		/// This was a local function rescheduling itself through a captured lambda — the shape a data record
		/// cannot express, which is what <see cref="OnContinuation"/> is for.
		/// </para>
		/// </summary>
		private void Poll(NodeRunner runner) {
			if (runner == null) {
				return;
			}

			var subRunner = NarramancerSingleton.Instance.GetNodeRunner(runner.Blackboard.GetString(SubRunnerKey));

			if (!runner.Blackboard.GetBool(RunningKey)) {
				NarramancerSingleton.Instance.ReleaseNodeRunner(subRunner);

				if (TryGetRunnableNodeFromPort(nameof(runNodeOnBranchCompleted), out var completedNode)) {
					runner.Prepend(completedNode);
				}

				runner.Resume();
				return;
			}

			var chosenBranch = BranchAt(runner.Blackboard.GetInt(ChosenBranchKey));
			var furtherChosenBranch = ChooseFirstTrueBranch(runner.Blackboard);

			if (furtherChosenBranch != null && furtherChosenBranch == chosenBranch) {
				NarramancerSingleton.Instance
					.MakeTimer(refreshDelay, TimerKey(runner))
					.WhenDone(Continuation.NodeCallbackTo(this, PollCallbackId, runner));
				return;
			}

			StopSubRunner(runner, subRunner);

			if (furtherChosenBranch != null) {
				// A different branch won: rerun this node from the top so the conditionals are recomputed
				// properly, rather than trying to switch in place.
				runner.Prepend(this);

				if (TryGetRunnableNodeFromPort(nameof(runNodeOnCancel), out var cancelNode)) {
					runner.Prepend(cancelNode);
				}
			}
			else {
				if (TryGetRunnableNodeFromPort(nameof(runNodeOnAllFalse), out var allFalseNode)) {
					runner.Prepend(allFalseNode);
				}
			}

			runner.Resume();
		}

		private static void StopSubRunner(NodeRunner runner, NodeRunner subRunner) {
			if (subRunner != null) {
				subRunner.StopAndReset();
				NarramancerSingleton.Instance.ReleaseNodeRunner(subRunner);
			}
		}

		public override void OnContinuation(string callbackId, NodeRunner runner) {
			if (callbackId == PollCallbackId) {
				Poll(runner);
			}
		}

		/// <summary>
		/// Only this node's own flag needs clearing. Stopping and releasing the sub-runner is
		/// <see cref="NodeRunner.StopAndReset"/>'s job now that the runner owns it — which also fixes the case
		/// this used to miss, where after a save/load the name lookup returned null and the sub-runner was
		/// left running.
		/// </summary>
		public override void Cancel(NodeRunner runner) {
			base.Cancel(runner);
			runner.Blackboard.Set(Blackboard.UniqueKey(this, "Running"), false);
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