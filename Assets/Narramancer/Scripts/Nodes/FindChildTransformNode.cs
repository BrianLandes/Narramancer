
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("GameObject/Find Child Transform")]
	public class FindChildTransformNode : Node {

		[Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		private GameObject gameObject = default;

		[Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField, HideLabel]
		private string transformName = "";

		[Output]
		[SerializeField]
		private Transform objectPosition = default;

		public override object GetValue(object context, NodePort port) {
			if (Application.isPlaying && port.fieldName.Equals(nameof(objectPosition))) {
				var inputGameObject = GetInputValue(context, nameof(gameObject), gameObject);
				if (inputGameObject != null) {
					var transformName = GetInputValue(context, nameof(this.transformName), this.transformName);
					return inputGameObject.transform.Find(transformName);
				}
			}
			return null;
		}

	}
}
