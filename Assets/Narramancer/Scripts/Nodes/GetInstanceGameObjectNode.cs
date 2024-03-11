using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	public class GetInstanceGameObjectNode : AbstractInstanceInputNode {

		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
		[SerializeField]
		GameObject targetGameObject = default;

		public override object GetValue(INodeContext context, NodePort port) {

			if (port.fieldName.Equals(nameof(targetGameObject))) {
				var instance = GetInstance(context);
				return instance?.GameObject;
			}

			return base.GetValue(context, port);
		}
	}
}
