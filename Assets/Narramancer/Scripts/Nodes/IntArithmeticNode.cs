
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
    [CreateNodeMenu("Math/Arithmetic (Ints)")]
    public class IntArithmeticNode : Node {

        [Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
        [SerializeField]
        protected int a = 0;

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
        protected int b = 0;

        [Output(connectionType = ConnectionType.Multiple, typeConstraint = TypeConstraint.Inherited, backingValue = ShowBackingValue.Never)]
        [SerializeField]
        protected int result;

        public override object GetValue(INodeContext context, NodePort port) {
            if (port.fieldName.Equals(nameof(result))) {
                int leftValue = GetInputValue(context, nameof(a), a);
                int rightValue = GetInputValue(context, nameof(b), b);

				switch (operation) {
					case Operation.Add:
                        return leftValue + rightValue;

                    case Operation.Subtract:
                        return leftValue - rightValue;

                    case Operation.Multiply:
                        return leftValue * rightValue;

                    case Operation.Divide:
                        if ( rightValue == 0 ) {
                            throw new System.Exception("Denominator was zero.");
						}
                        return leftValue / rightValue;
				}
			}
            return null;
		}
	}
}
