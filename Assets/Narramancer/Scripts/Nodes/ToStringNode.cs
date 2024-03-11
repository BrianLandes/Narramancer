using UnityEngine;
using XNode;

namespace Narramancer {

	public class ToStringNode : ResizableNode {


		[SerializeField]
		private SerializableType objectType = new SerializableType();
		public SerializableType ObjectType => objectType;

		private const string ELEMENT = "Element";

		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Strict)]
		[SerializeField]
		private string result = "";

		protected override void Init() {
			objectType.OnChanged -= UpdatePorts;
			objectType.OnChanged += UpdatePorts;
		}

		public override void UpdatePorts() {



			if (objectType.Type == null) {
				ClearDynamicPorts();
			}
			else {
				var port = this.GetOrAddDynamicInput(objectType.Type, ELEMENT);
				this.ClearDynamicPortsExcept(port);
			}

			base.UpdatePorts();
		}

		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying && port.fieldName.Equals(nameof(result))) {
				var inputPort = GetInputPort(ELEMENT);
				var input = inputPort.GetInputValue(context);
				if (input != null) {
					return input.ToString();
				}
			}
			return null;
		}
	}
}