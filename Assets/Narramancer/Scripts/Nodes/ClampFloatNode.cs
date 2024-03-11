
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
    [CreateNodeMenu("Math/Clamp (Float)")]
    public class ClampFloatNode : Node {

        [Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
        [SerializeField]
        protected float value = 0;

        [Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
        [SerializeField]
        protected float min = 0;

        [Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
        [SerializeField]
        protected float max = 1;

        [Output(connectionType = ConnectionType.Multiple, typeConstraint = TypeConstraint.Inherited, backingValue = ShowBackingValue.Never)]
        [SerializeField]
        protected float result;

        public override object GetValue(INodeContext context, NodePort port) {
            if (port.fieldName.Equals(nameof(result))) {
                float inputValue = GetInputValue(context, nameof(value), value);
                float minValue = GetInputValue(context, nameof(min), min);
                float maxValue = GetInputValue(context, nameof(max), max);

                return Mathf.Clamp(inputValue, minValue, maxValue);
            }
            return null;
        }
    }
}
