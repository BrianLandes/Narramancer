using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {

	[NodeWidth(400)]
	[CreateNodeMenu("Logic/Is Not Null")]
	public class IsNotNullNode : ResizableNode {

		[SerializeField]
		private SerializableType type = new SerializableType();
		public SerializableType ObjectType => type;

		private static string TARGET_OBJECT = "Object";

		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
		[SerializeField]
		private bool isNotNull = false;

		protected override void Init() {
			type.OnChanged -= UpdatePorts;
			type.OnChanged += UpdatePorts;
		}

		public override void UpdatePorts() {

			if (type.Type == null) {
				ClearDynamicPorts();
			}
			else {
				List<NodePort> existingPorts = new List<NodePort>();

				var leftInputPort = this.GetOrAddDynamicInput(type.Type, TARGET_OBJECT);
				existingPorts.Add(leftInputPort);

				this.ClearDynamicPortsExcept(existingPorts);
			}

			base.UpdatePorts();
		}


		public override object GetValue(INodeContext context, NodePort port) {
			if (type.Type == null) {
				return null;
			}
			switch (port.fieldName) {
				case nameof(isNotNull):

					var leftPort = this.GetDynamicInput(type.Type, TARGET_OBJECT);

					var left = leftPort.GetInputValue(context);

					return left != null;
			}
			return null;
		}
	}
}