#if NARRAMANCER_AI_MODULE
using System;
using UnityEngine;
using UnityEngine.AI;

namespace Narramancer {

	/// <summary>
	/// A navigation agent's settings and where it was heading. Ported from <c>SerializeNavMeshAgent</c> in the
	/// Pseudo World Gaia fork, minus the move-and-wait behaviour, which is <see cref="NavMeshMover"/>'s job.
	/// <para>
	/// Compiled only when the built-in AI module is enabled (<c>NARRAMANCER_AI_MODULE</c>, set by the asmdef),
	/// so a project that has removed the module still compiles.
	/// </para>
	/// </summary>
	public class NavMeshAgentSaver : ComponentSaver<NavMeshAgent, NavMeshAgentSaver.State> {

		[Serializable]
		public class State {
			public bool enabled;
			public float speed;
			public float stoppingDistance;
			public bool isStopped;
			public bool hasDestination;
			public Vector3 destination;
		}

		public override string Key => "navMeshAgent";

		public override State Capture(NavMeshAgent agent) {
			var state = new State {
				enabled = agent.enabled,
				speed = agent.speed,
				stoppingDistance = agent.stoppingDistance,
			};
			// isStopped and destination throw on an agent that is disabled or off the mesh.
			if (agent.isActiveAndEnabled && agent.isOnNavMesh) {
				state.isStopped = agent.isStopped;
				state.hasDestination = agent.hasPath || agent.pathPending;
				state.destination = agent.destination;
			}
			return state;
		}

		public override void Apply(NavMeshAgent agent, State state) {
			agent.enabled = state.enabled;
			agent.speed = state.speed;
			agent.stoppingDistance = state.stoppingDistance;
			if (!agent.isActiveAndEnabled) {
				return;
			}
			// The transform driver has already put the object back where it was saved, but an agent keeps its own
			// idea of position and pulls the transform back to it. Warp moves the agent itself.
			agent.Warp(agent.transform.position);
			if (agent.isOnNavMesh) {
				agent.isStopped = state.isStopped;
				if (state.hasDestination) {
					agent.SetDestination(state.destination);
				}
			}
		}
	}
}
#endif
