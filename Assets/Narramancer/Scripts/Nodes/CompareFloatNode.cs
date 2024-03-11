
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
    [CreateNodeMenu("Math/Compare (Floats)")]
    public class CompareFloatNode : Node {

        [Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
        [SerializeField]
        protected float a = 0;

        [Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
        [SerializeField]
        protected float b = 0;

        [SerializeField, NodeEnum]
        protected Comparison comparison;

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

					case Comparison.NotEqualTo:
                        return !Mathf.Approximately(leftValue, rightValue);

                    case Comparison.LessThan:
                        return leftValue < rightValue;

                    case Comparison.GreaterThan:
                        return leftValue > rightValue;
				}
			}
            return null;
		}
	}
}
