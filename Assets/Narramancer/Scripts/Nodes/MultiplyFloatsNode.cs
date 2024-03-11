
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
    [CreateNodeMenu("Math/Multiply Floats")]
    public class MultiplyFloatsNode : Node {

        [Input(connectionType = ConnectionType.Multiple, typeConstraint = TypeConstraint.Inherited, backingValue = ShowBackingValue.Never)]
        [SerializeField]
        protected float values = 0;

        [Output(connectionType = ConnectionType.Multiple, typeConstraint = TypeConstraint.Inherited, backingValue = ShowBackingValue.Never)]
        [SerializeField]
        protected float product;

        public override object GetValue(INodeContext context, NodePort port) {
            if (port.fieldName.Equals(nameof(product))) {
                var inputValues = GetInputValues<float>(context, nameof(values));

                if (inputValues.Length == 0) {
                    return 0;
                }

                float product = 1f;

                foreach (var value in inputValues) {
                    product *= value;
                }

                return product;
            }
            return null;
        }
    }
}
