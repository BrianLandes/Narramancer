
using System;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("List/List Count")]
	public class ListCountNode : Node, IListTypeNode {

		[SerializeField]
		private SerializableType listType = new SerializableType();
		public SerializableType ListType => listType;

		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Strict)]
		[SerializeField]
		private int count = 0;

		private const string LIST = "List";

		protected override void Init() {
			listType.OnChanged -= UpdatePorts;
			listType.OnChanged += UpdatePorts;
		}

		public override void UpdatePorts() {
			if (listType.Type == null) {
				ClearDynamicPorts();
			}
			else {
				var inputPort = this.GetOrAddDynamicInput(listType.TypeAsList, LIST);
				this.ClearDynamicPortsExcept(inputPort);
			}

			base.UpdatePorts();
		}

		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying && port.fieldName.Equals(nameof(count))) {
				var inputPort = GetInputPort(LIST);
				var inputValue = inputPort.GetInputValue(context);

				Type type = inputValue.GetType();
				var propertyInfo = type.GetProperty("Count");
				var count = propertyInfo.GetValue(inputValue);
				return count;
			}
			return null;
		}
	}
}