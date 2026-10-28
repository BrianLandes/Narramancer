#if NARRAMANCER_AI_MODULE
using UnityEngine;
using UnityEngine.AI;

namespace Narramancer {

	/// <summary>
	/// Walks a <see cref="NavMeshAgent"/> to a point or a moving target and resolves a promise on arrival,
	/// surviving a save part-way through. Ported from <c>SerializeNavMeshAgent</c> in the Pseudo World Gaia fork;
	/// the agent's own settings are the <see cref="NavMeshAgentSaver"/> driver's.
	/// <para>
	/// The fork saved a target transform by its hierarchy path and found it again with <c>GameObject.Find</c> —
	/// the same path identity that broke when anything was renamed. The target is now a saved reference, which
	/// needs a <see cref="Saveable"/> on the target.
	/// </para>
	/// </summary>
	[RequireComponent(typeof(NavMeshAgent))]
	public class NavMeshMover : MonoBehaviour {

		[Save]
		private Promise promise = default;

		[Save]
		private bool moving = false;

		[Save]
		private Transform target = null;

		public static NavMeshMover On(NavMeshAgent agent) {
			if (!agent.TryGetComponent<NavMeshMover>(out var mover)) {
				mover = agent.gameObject.AddComponent<NavMeshMover>();
			}
			return mover;
		}

		public Promise MoveTo(Vector3 destination) {
			return Begin(null, destination);
		}

		public Promise MoveTo(Transform target) {
			return Begin(target, target.position);
		}

		private Promise Begin(Transform target, Vector3 destination) {
			promise?.Resolve();
			this.target = target;
			GetComponent<NavMeshAgent>().destination = destination;
			moving = true;
			promise = new Promise();
			return promise;
		}

		private void Update() {
			var agent = GetComponent<NavMeshAgent>();
			if (!moving || !agent.enabled) {
				return;
			}
			if (target != null) {
				agent.destination = target.position;
			}
			var distanceSquared = (agent.transform.position - agent.destination).sqrMagnitude;
			var threshold = Mathf.Max(0.001f, agent.stoppingDistance * agent.stoppingDistance);
			if (distanceSquared <= threshold) {
				moving = false;
				target = null;
				var resolved = promise;
				promise = null;
				resolved?.Resolve();
			}
		}
	}
}
#endif
