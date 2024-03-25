using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Narramancer {
	public class DestroyGameObjectNode : ChainedRunnableNode {

		[Input(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField]
		GameObject targetGameObject = default;

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			var targetGameObject = GetInputValue(runner.Blackboard, nameof(this.targetGameObject), this.targetGameObject);

			Destroy(targetGameObject);

			var instance = NarramancerSingleton.Instance.GetInstances().FirstOrDefault(instance => instance.GameObject == targetGameObject);
			if (instance != null) {
				instance.GameObject = null;
			}
		}
	}
}
