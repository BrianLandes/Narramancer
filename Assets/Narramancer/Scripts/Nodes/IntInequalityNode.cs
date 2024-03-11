
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
    [CreateNodeMenu("Math/Inequality (Ints)")]

    public class IntInequalityNode : Node {

        [Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
        [SerializeField]
        protected int a = 0;

        public enum Comparison {
            EqualTo,
            GreaterThan,
            LessThan,
            GreaterThanOrEqualTo,
            LessThanOrEqualTo
        }
        [SerializeField, NodeEnum, HideLabel]
        [Tooltip("A is {comparison} B")]
        protected Comparison comparison;


        [Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
        [SerializeField]
        protected int b = 0;

        [Output(connectionType = ConnectionType.Multiple, typeConstraint = TypeConstraint.Inherited, backingValue = ShowBackingValue.Never)]
        [SerializeField]
        protected bool result;

        public override object GetValue(INodeContext context, NodePort port) {
            if (port.fieldName.Equals(nameof(result))) {
                float leftValue = GetInputValue(context, nameof(a), a);
                float rightValue = GetInputValue(context, nameof(b), b);

				switch (comparison) {
					case Comparison.EqualTo:
						return Mathf.Approximately(leftValue, rightValue);

					case Comparison.GreaterThan:
						return leftValue > rightValue;

					case Comparison.LessThan:
						return leftValue < rightValue;

					case Comparison.GreaterThanOrEqualTo:
                        return leftValue >= rightValue;

                    case Comparison.LessThanOrEqualTo:
                        return leftValue <= rightValue;
                }
			}
            return null;
		}
	}
}
