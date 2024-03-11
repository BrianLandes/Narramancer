
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("GameObject/Find Child Transform")]
	public class FindChildTransformNode : Node {

		[Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		private GameObject targetGameObject = default;

		[Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField, HideLabel]
		private string transformName = "";

		[Output]
		[SerializeField]
		private Transform transform = default;

		[Output]
		[SerializeField]
		private GameObject gameObject = default;

		[Output]
		[SerializeField]
		private Vector3 worldPosition = default;

		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying) {

				var inputGameObject = GetInputValue(context, nameof(targetGameObject), targetGameObject);
				if (inputGameObject != null) {
					var transformName = GetInputValue(context, nameof(this.transformName), this.transformName);

					var child = inputGameObject.transform.Find(transformName);
					switch (port.fieldName) {
						case nameof(transform):
							return child;
						case nameof(gameObject):
							return child?.gameObject;
						case nameof(worldPosition):
							return child?.transform.position;
					}

				}
			}
			return null;
		}

	}
}
