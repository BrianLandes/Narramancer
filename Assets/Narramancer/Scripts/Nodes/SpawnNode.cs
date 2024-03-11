using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
    public class SpawnNode : ChainedRunnableNode {

		[Input(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField]
		SerializableSpawner spawner;

		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
		[SerializeField]
		GameObject gameObject;

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			var spawner = GetInputValue(runner.Blackboard, nameof(this.spawner), this.spawner);
			if (spawner == null) {
				Debug.LogError("Spawner was null", this);
				return;
			}
			var result = spawner.Spawn();
			var key = Blackboard.UniqueKey(this, nameof(gameObject));
			runner.Blackboard.Set(key, result);
		}

		public override object GetValue(INodeContext context, NodePort port) {
			if (port.fieldName.Equals(nameof(gameObject))) {
				var blackboard = context as Blackboard;
				var key = Blackboard.UniqueKey(this, nameof(gameObject));
				var value = blackboard.Get<GameObject>(key);
				return value;
			}
			return base.GetValue(context, port);
		}
	}
}