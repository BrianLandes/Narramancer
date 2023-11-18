using UnityEngine;
using XNode;

namespace Narramancer {
	public abstract class AbstractCallMethodOnSpecificTypeValueNode<T> : AbstractDynamicMethodValueNode where T : class {

		[Input(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Strict)]
		[SerializeField]
		[HideInInspector]
		private T inputValue = default;

		[Output]
		[SerializeField]
		[HideInInspector]
		private T passThroughValue = default;

		protected override void Init() {
			base.Init();
			method.LookupTypes = new[] { typeof(T) };
		}

		protected override object GetTargetObject(object context) {
			return GetInputValue<T>(context, nameof(inputValue));
		}

		public override object GetValue(object context, NodePort port) {
			if (Application.isPlaying) {
				if (port.fieldName.Equals(nameof(passThroughValue))) {
					return GetTargetObject(context);
				}
			}

			return base.GetValue(context, port);
		}

	}
}