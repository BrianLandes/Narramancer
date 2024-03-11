
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
    [CreateNodeMenu("Math/Sum Floats")]

    public class SumFloatsNode : Node {

        [Input(connectionType = ConnectionType.Multiple, typeConstraint = TypeConstraint.Inherited, backingValue = ShowBackingValue.Never)]
        [SerializeField]
        protected float values = 0;

        [Output(connectionType = ConnectionType.Multiple, typeConstraint = TypeConstraint.Inherited, backingValue = ShowBackingValue.Never)]
        [SerializeField]
        protected float result;

        public override object GetValue(INodeContext context, NodePort port) {
            if (port.fieldName.Equals(nameof(result))) {
                var inputValues = GetInputValues<float>(context, nameof(values));

                if (inputValues.Length == 0) {
                    return 0;
                }

                float result = 0f;

                foreach (var value in inputValues) {
                    result += value;
                }

                return result;
            }
            return null;
        }
    }
}
