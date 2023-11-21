using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Narramancer {
	public class SetInstanceGameObjectNode : AbstractInstanceInputChainedRunnableNode {

		[Input(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField]
		GameObject targetGameObject = default;

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			var instance = GetInstance(runner.Blackboard);

			var targetGameObject = GetInputValue(runner.Blackboard, nameof(this.targetGameObject), this.targetGameObject);

			if (instance!=null) {

				instance.GameObject = targetGameObject;
			}
		}
	}
}
