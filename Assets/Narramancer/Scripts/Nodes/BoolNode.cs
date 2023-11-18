
using UnityEngine;
using XNode;

namespace Narramancer {
	[NodeWidth(70)]
	[CreateNodeMenu("Logic/Bool")]
	public class BoolNode : Node {

		[SerializeField]
		[HideLabel]
		protected bool value = true;


		[Output(connectionType = ConnectionType.Multiple, typeConstraint = TypeConstraint.Inherited, backingValue = ShowBackingValue.Never)]
		[SerializeField]
		protected bool result;

		public override object GetValue(object context, NodePort port) {
			if (port.fieldName.Equals(nameof(result))) {
				return value;
			}
			return null;
		}

	}
}
