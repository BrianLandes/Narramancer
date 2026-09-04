
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
    [CreateNodeMenu("Math/Inequality (Floats)")]

    public class InequalityNode : Node {

        [Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
        [SerializeField]
        protected float a = 0;

        /// <summary>
        /// The order here is load-bearing and must not be rearranged. Unity serializes an enum field by its
        /// <b>integer</b> value, so an authored graph stores the index, not the name.
        /// <para>
        /// Until 2026-09-04 there were three members and the middle two were misnamed: <c>GreaterThan</c>
        /// evaluated <c>&gt;=</c> and <c>LessThan</c> evaluated <c>&lt;=</c>. Renaming them in place and
        /// appending the two strict operators makes every existing graph keep its exact behaviour — index 1
        /// still means <c>&gt;=</c>, index 2 still means <c>&lt;=</c> — while the label finally matches what
        /// runs. Inserting the new members anywhere but the end would silently change the meaning of every
        /// authored comparison.
        /// </para>
        /// </summary>
        public enum Comparison {
            ApproximatelyEqualTo = 0,
            GreaterThanOrEqualTo = 1,
            LessThanOrEqualTo = 2,
            GreaterThan = 3,
            LessThan = 4,
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
                        return Mathf.Approximately(leftValue, rightValue);

                    case Comparison.GreaterThanOrEqualTo:
                        return leftValue >= rightValue;

                    case Comparison.LessThanOrEqualTo:
                        return leftValue <= rightValue;

                    case Comparison.GreaterThan:
                        return leftValue > rightValue;

                    case Comparison.LessThan:
                        return leftValue < rightValue;

                }
            }
            return null;
        }
    }
}
