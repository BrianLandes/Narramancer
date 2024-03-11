
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
    [CreateNodeMenu("Math/Int")]

    public class IntLiteralNode : Node {

        [Output(connectionType = ConnectionType.Multiple, typeConstraint = TypeConstraint.Inherited, backingValue = ShowBackingValue.Always)]
        [SerializeField, HideLabel]
        protected int value = 1;

        public override object GetValue(INodeContext context, NodePort port) {
            return value;
        }
    }
}
