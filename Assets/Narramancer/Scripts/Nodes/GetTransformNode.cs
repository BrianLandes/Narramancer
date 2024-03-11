using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {

	public class GetTransformNode : Node {

		[Input(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField]
		GameObject gameObject;

		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
		[SerializeField]
		[SameLine]
		Transform transform;

		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
		[SerializeField]
		RectTransform rectTransform;

		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
		[SerializeField]
		Vector3 worldPosition;

		public override object GetValue(INodeContext context, NodePort port) {
			var gameObject = GetInputValue(context, nameof(this.gameObject), this.gameObject);
			if (gameObject != null) {
				switch (port.fieldName) {
					case nameof(transform):
						return gameObject.transform;
					case nameof(rectTransform):
						return gameObject.GetComponent<RectTransform>();
					case nameof(worldPosition):
						return gameObject.transform.position;
				}
				
			}
			return null;
		}
	}
}