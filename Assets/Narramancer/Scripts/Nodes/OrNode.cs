
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {

	[NodeWidth(70)]
	[CreateNodeMenu("Logic/Or")]
	public class OrNode : Node {
		[Input(connectionType = ConnectionType.Multiple, typeConstraint = TypeConstraint.Inherited, backingValue = ShowBackingValue.Never)]
		[SerializeField]
		protected bool values = true;


		[Output(connectionType = ConnectionType.Multiple, typeConstraint = TypeConstraint.Inherited, backingValue = ShowBackingValue.Never)]
		[SerializeField]
		protected bool result;

		public override object GetValue(object context, NodePort port) {
			if (Application.isPlaying && port.fieldName.Equals(nameof(result))) {

				var inputPort = GetInputPort(nameof(values));

				if (!inputPort.IsConnected) {
					return true;
				}

				var orderedConnections = inputPort.GetConnections().OrderBy(nodePort => nodePort.node.position.y);

				foreach (var connection in orderedConnections) {
					var value = (bool)connection.GetOutputValue(context);
					if (value) {
						return true;
					}
				}

				return false;
			}
			return null;
		}

	}
}
