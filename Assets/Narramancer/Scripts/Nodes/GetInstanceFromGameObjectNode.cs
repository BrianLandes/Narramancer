using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	public class GetInstanceFromGameObjectNode : Node {

		[Input(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField]
		GameObject targetGameObject = default;

		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
		[SerializeField]
		NounInstance instance = default;

		public override object GetValue(INodeContext context, NodePort port) {

			if (port.fieldName.Equals(nameof(instance))) {
				var targetGameObject = GetInputValue(context, nameof(this.targetGameObject), this.targetGameObject);
				var nounReference = targetGameObject?.GetComponentInChildren<SerializeNounInstanceReference>();

				var instance = nounReference?.GetInstance();
				return instance;
			}

			return null;
		}
	}
}
