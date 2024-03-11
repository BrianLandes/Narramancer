using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	public class CallMethodOnScriptableObjectRunnableNode : AbstractDynamicMethodRunnableNode {

		[Input]
		[SerializeField]
		public ScriptableObject targetObject;

		protected override void Init() {
			base.Init();
			method.LookupTypes = null;
			method.GetLookupTypes = () => {
				var target = GetInputValue(null, nameof(targetObject), targetObject);
				if ( target == null) {
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