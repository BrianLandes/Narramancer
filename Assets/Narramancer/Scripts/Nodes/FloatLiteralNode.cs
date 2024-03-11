
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
    [CreateNodeMenu("Math/Float")]
    public class FloatLiteralNode : Node {

        [Output(connectionType = ConnectionType.Multiple, typeConstraint = TypeConstraint.Inherited, backingValue =ShowBackingValue.Always)]
        [SerializeField, HideLabel]
        protected float value = 1;

        public override object GetValue(INodeContext context, NodePort port) {
            if (port.fieldName.Equals(nameof(value))) {
                return value;
            }
            return null;
        }
    }
}
