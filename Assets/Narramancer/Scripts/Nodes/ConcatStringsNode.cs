using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	public class ConcatStringsNode : Node {

		[Input(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Strict)]
		[SerializeField]
		private List<string> list = default;


		[Input(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Strict)]
		[SerializeField]
		private string elements = "";

		public enum SeparatorType {
			Comma,
			NewLine,
			Tab,
			Custom
		}

		[NodeEnum]
		[SerializeField]
		private SeparatorType separatorType = SeparatorType.Comma;

		//TODO: hide when type is not Custom
		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Strict)]
		[SerializeField]
		private string separator = ", ";


		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Strict)]
		[SerializeField]
		private string result = "";

		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying && port.fieldName.Equals(nameof(result))) {

				var values = new List<string>();

				var list = this.GetInputValueList<string>(context, nameof(this.list));
				if (list != null) {
					values.AddRange(list.WithoutNulls());
				}

				var inputValues = GetInputValues<string>(context, nameof(elements));
				values.AddRange(inputValues.WithoutNulls());

				var separator = GetInputValue(context, nameof(this.separator), this.separator);

				switch (separatorType) {
					case SeparatorType.Comma:
						separator = ", ";
						break;
					case SeparatorType.NewLine:
						separator = "\n";
						break;
					case SeparatorType.Tab:
						separator = "\t";
						break;
					case SeparatorType.Custom:
						// leave it as is
						break;
				}

				return string.Join(separator, values);
			}
			return null;
		}
	}
}