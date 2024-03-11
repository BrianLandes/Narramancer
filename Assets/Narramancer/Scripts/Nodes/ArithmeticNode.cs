using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("Math/Arithmetic (Floats)")]
	public class ArithmeticNode : Node {

		[Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		protected float a = 0;

		public enum Operation {
			Add,
			Subtract,
			Multiply,
			Divide
		}
		[SerializeField, NodeEnum]
		protected Operation operation;

		[Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		protected float b = 0;

		[Output(connectionType = ConnectionType.Multiple, typeConstraint = TypeConstraint.Inherited, backingValue = ShowBackingValue.Never)]
		[SerializeField]
		protected float result;

		public override object GetValue(INodeContext context, NodePort port) {
			if (port.fieldName.Equals(nameof(result))) {
				float leftValue = GetInputValue(context, nameof(a), a);
				float rightValue = GetInputValue(context, nameof(b), b);

				switch (operation) {
					case Operation.Add:
						return leftValue + rightValue;

					case Operation.Subtract:
						return leftValue - rightValue;

					case Operation.Multiply:
						return leftValue * rightValue;

					case Operation.Divide:
						if (rightValue == 0) {
							throw new System.Exception("Denominator was zero.");
						}
						return leftValue / rightValue;
				}
			}
			return null;
		}
	}
}
