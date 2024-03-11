
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
    [CreateNodeMenu("Math/Inequality (Floats)")]

    public class InequalityNode : Node {

        [Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
        [SerializeField]
        protected float a = 0;

        public enum Comparison {
            ApproximatelyEqualTo,
            GreaterThan,
            LessThan
        }
        [SerializeField, NodeEnum]
        [Tooltip("A is {comparison} B")]
        protected Comparison comparison;

        [Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
        [SerializeField]
        protected float b = 0;

        [Output(connectionType = ConnectionType.Multiple, typeConstraint = TypeConstraint.Inherited, backingValue = ShowBackingValue.Never)]
        [SerializeField]
        protected bool result;

        public override object GetValue(INodeContext context, NodePort port) {
            if (port.fieldName.Equals(nameof(result))) {
                float leftValue = GetInputValue(context, nameof(a), a);
                float rightValue = GetInputValue(context, nameof(b), b);

                switch (comparison) {
                    case Comparison.ApproximatelyEqualTo:
                        return Mathf.Approximately( leftValue, rightValue);

                    case Comparison.GreaterThan:
                        return leftValue >= rightValue;

                    case Comparison.LessThan:
                        return leftValue <= rightValue;

                }
            }
            return null;
        }
    }
}
