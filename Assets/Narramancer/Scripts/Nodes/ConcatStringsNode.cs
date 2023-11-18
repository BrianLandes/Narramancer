using UnityEngine;
using XNode;

namespace Narramancer {
	public class ConcatStringsNode : Node {


		[Input(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Strict)]
		[SerializeField]
		private string elements = "";

		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Strict)]
		[SerializeField]
		private string result = "";

		public override object GetValue(object context, NodePort port) {
			if (Application.isPlaying && port.fieldName.Equals(nameof(result))) {
				var inputValues = GetInputValues<string>(context, nameof(elements));
				return inputValues.CommaSeparated();
			}
			return null;
		}
	}
}