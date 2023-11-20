
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

		public override object GetValue(object context, NodePort port) {
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
					}

				}
			}
			return null;
		}

	}
}
