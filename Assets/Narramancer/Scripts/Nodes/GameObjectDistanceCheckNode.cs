
using System;
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("Math/GameObject Distance Check")]
	public class GameObjectDistanceCheckNode : Node {

		[Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		private GameObject gameObjectA = default;

		[Serializable]
		public enum Operation {
			LessThan,
			GreaterThan,
			Approximately
		}
		[NodeEnum]
		[SerializeField]
		Operation operation = Operation.Approximately;


		[Input( ShowBackingValue.Unconnected, ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		private float distance = 6;

		[Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		private GameObject gameObjectB = default;

		[Output]
		[SerializeField]
		private bool result = false;

		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying && port.fieldName.Equals(nameof(this.result))) {

				var gameObjectA = GetInputValue(context, nameof(this.gameObjectA), this.gameObjectA);
				var gameObjectB = GetInputValue(context, nameof(this.gameObjectB), this.gameObjectB);

				var distance = GetInputValue(context, nameof(this.distance), this.distance);
				var sqrDistance = distance * distance;

				var sqrMagnitude = (gameObjectA.transform.position - gameObjectB.transform.position).sqrMagnitude;

				switch (operation) {
					case Operation.LessThan:
						return sqrMagnitude < sqrDistance;
					case Operation.GreaterThan:
						return sqrMagnitude > sqrDistance;
					case Operation.Approximately:
						return Mathf.Approximately( sqrMagnitude, sqrDistance);
				}

			}
			return null;
		}
	}
}
