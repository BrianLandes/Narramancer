
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {

    [NodeWidth(130)]
    [CreateNodeMenu("Math/Int to Float")]
    public class IntToFloatNode : Node {

        [Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
        [SerializeField]
        protected int value = 0;

        [Output(connectionType = ConnectionType.Multiple, typeConstraint = TypeConstraint.Inherited, backingValue = ShowBackingValue.Never)]
        [SerializeField]
        [SameLine]
        protected float result;

        public override object GetValue(INodeContext context, NodePort port) {
            if (port.fieldName.Equals(nameof(result))) {
                return (float)GetInputValue(context, nameof(value), value);
            }
            return null;
        }
    }
}
