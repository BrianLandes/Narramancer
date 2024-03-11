
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("Math/Choose (Int)")]
	public class ChooseIntNode : Node {

		[Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		protected bool condition = true;

		[Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		protected int valueWhenTrue = 0;

		[Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		protected int valueWhenFalse = 0;

		[Output(connectionType = ConnectionType.Multiple, typeConstraint = TypeConstraint.Inherited, backingValue = ShowBackingValue.Never)]
		[SerializeField]
		protected int result;

		public override object GetValue(INodeContext context, NodePort port) {
			if (port.fieldName.Equals(nameof(result))) {

				bool inputCondition = GetInputValue(context, nameof(condition), condition);

				if (inputCondition) {
					var inputValue = GetInputValue(context, nameof(valueWhenTrue), valueWhenTrue);
					return inputValue;
				}
				else {
					var inputValue = GetInputValue(context, nameof(valueWhenFalse), valueWhenFalse);
					return inputValue;
				}

			}
			return null;
		}
	}
}
