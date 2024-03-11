using UnityEngine;
using XNode;

namespace Narramancer {

	[CreateNodeMenu("Noun/Get Instance Display Name")]
	public class GetDisplayNameNode : AbstractInstanceInputNode {

		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
		[SerializeField]
		string displayName = default;

		public override object GetValue(INodeContext context, NodePort port) {

			if (port.fieldName.Equals(nameof(displayName))) {
				var instance = GetInstance(context);
				return instance?.DisplayName;
			}

			return base.GetValue(context, port);
		}
	}
}
