
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	[NodeWidth(60)]
	[CreateNodeMenu("Logic/Not")]
	public class NotNode : Node {
		[Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited, backingValue = ShowBackingValue.Never)]
		[SerializeField]
		protected bool value;


		[Output(connectionType = ConnectionType.Multiple, typeConstraint = TypeConstraint.Inherited, backingValue = ShowBackingValue.Never)]
		[SerializeField, HideLabel, SameLine]
		protected bool result;

		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying && port.fieldName.Equals(nameof(result))) {
				var inputValue = GetInputValue<bool>(context, nameof(value));
				return !inputValue;
			}
			return null;
		}

	}
}

