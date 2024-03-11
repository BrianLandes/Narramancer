
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {

    [NodeWidth(130)]
    [CreateNodeMenu("Math/Float to Int")]
    public class FloatToIntNode : Node {

        [Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
        [SerializeField]
        protected float value = 0;

        [Output(connectionType = ConnectionType.Multiple, typeConstraint = TypeConstraint.Inherited, backingValue = ShowBackingValue.Never)]
        [SerializeField]
        [SameLine]
        protected int result;

        public override object GetValue(INodeContext context, NodePort port) {
            if (port.fieldName.Equals(nameof(result))) {
                return Mathf.RoundToInt( GetInputValue(context, nameof(value), value));
            }
            return null;
        }
    }
}
