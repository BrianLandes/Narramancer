using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {

	[CreateNodeMenu("GameObject/Get Camera")]
	public class GetCameraNode : Node {
		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
		[SerializeField]
		Camera camera = default;

		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
		[SerializeField]
		Transform transform = default;

		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
		[SerializeField]
		GameObject gameObject = default;

		public override object GetValue(INodeContext context, NodePort port) {
			switch (port.fieldName) {
				case nameof(camera):
					return Camera.main;
				case nameof(transform):
					return Camera.main.transform;
				case nameof(gameObject):
					return Camera.main.gameObject;

			}
			return null;
		}
	}
}
