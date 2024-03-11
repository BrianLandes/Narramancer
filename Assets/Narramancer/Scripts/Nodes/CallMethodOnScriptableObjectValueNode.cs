using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {

	public class CallMethodOnScriptableObjectValueNode : AbstractDynamicMethodValueNode {

		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField]
		protected ScriptableObject targetObject;

		protected override void Init() {
			base.Init();
			method.LookupTypes = null;
			method.GetLookupTypes = () => {
				var target = GetInputValue(null, nameof(targetObject), targetObject);
				if (target == null) {
					return null;
				}
				var type = target.GetType();
				return new[] { type };
			};
		}

		protected override object GetTargetObject(INodeContext context) {
			return GetInputValue(context, nameof(targetObject), targetObject);
		}

	}
}