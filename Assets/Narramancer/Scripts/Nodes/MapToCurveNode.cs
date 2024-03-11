
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
    [CreateNodeMenu("Math/Map Float To Curve")]
    public class MapToCurveNode : Node {

        [Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
        [SerializeField, HideLabel]
        protected float value = 0;

        [SerializeField, HideLabel]
        protected AnimationCurve curve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));

        [Output(connectionType = ConnectionType.Multiple, typeConstraint = TypeConstraint.Inherited, backingValue = ShowBackingValue.Never)]
        [SerializeField]
        protected float result;

        public override object GetValue(INodeContext context, NodePort port) {
            if (port.fieldName.Equals(nameof(result))) {
                float inputValue = GetInputValue(context, nameof(value), value);
                result = curve.Evaluate(inputValue);
                return result;
            }
            return null;
        }
    }
}
