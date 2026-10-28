#if NARRAMANCER_AI_MODULE
using UnityEngine;
using UnityEngine.AI;

namespace Narramancer {

	/// <summary>
	/// Walks a <see cref="NavMeshAgent"/> to a point, or after a moving transform, through
	/// <see cref="NavMeshMover"/> — so a save made mid-walk resumes the walk, and a verb waiting on the arrival
	/// carries on when it gets there.
	/// </summary>
	[CreateNodeMenu("GameObject/Move NavMesh Agent")]
	[NodeSearchTerms("Walk To", "Pathfind", "Navigate", "Move Agent")]
	public class MoveNavMeshAgentNode : ChainedRunnableNode {

		[Input(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField]
		private NavMeshAgent agent = default;
		public static string AgentFieldName => nameof(agent);

		[Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		private Vector3 destination = default;
		public static string DestinationFieldName => nameof(destination);

		[Input(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField]
		[Tooltip("If connected, walks to this transform instead of the destination, and keeps after it as it moves")]
		private Transform follow = default;
		public static string FollowFieldName => nameof(follow);

		[SerializeField]
		[Tooltip("Wait until the agent arrives before running the next node")]
		private bool wait = true;
		public static string WaitFieldName => nameof(wait);

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			var agent = GetInputValue(runner.Blackboard, nameof(this.agent), this.agent);
			if (agent == null) {
				Debug.LogWarning($"{name}: no NavMeshAgent to move.", this);
				return;
			}

			var follow = GetInputValue(runner.Blackboard, nameof(this.follow), this.follow);
			var mover = NavMeshMover.On(agent);
			var promise = follow != null
				? mover.MoveTo(follow)
				: mover.MoveTo(GetInputValue(runner.Blackboard, nameof(destination), destination));

			if (wait) {
				runner.Suspend();
				promise.WhenDone(Continuation.Resume(runner));
			}
		}
	}
}
#endif
