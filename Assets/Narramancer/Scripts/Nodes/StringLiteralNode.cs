using XNode;

namespace Narramancer {

	public class StringLiteralNode : Node {

		[Output(ShowBackingValue.Always, ConnectionType.Multiple, TypeConstraint.Strict)]
		[HideLabel]
		public string value;

		public override object GetValue(object context, NodePort port) {
			if (port.fieldName.Equals(nameof(value))) {
				return value;
			}
			return null;
		}

	}
}

