
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	[NodeWidth(400)]
	public class FormatStringNode : ResizableNode {

		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Strict)]
		[SerializeField]
		[TextArea]
		private string text = "";
		public static string TextFieldName => nameof(text);

		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Strict)]
		[SerializeField]
		private string result = "";
		public static string ResultFieldName => nameof(result);

		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying && port.fieldName.Equals(nameof(result))) {
				var inputText = GetInputValue(context, nameof(text), text);

				foreach (var input in DynamicInputs) {
					var inputObject = input.GetInputValue(context);
					if (inputObject != null) {
						inputText = inputText.Replace("{" + input.fieldName + "}", inputObject.ToString());
					}
				}

				return inputText;
			}
			return null;
		}

	}
}