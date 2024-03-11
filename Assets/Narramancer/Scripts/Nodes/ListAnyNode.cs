
using System;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("List/Any Elements in List")]
	public class ListAnyNode : Node, IListTypeNode {

		[SerializeField]
		private SerializableType listType = new SerializableType();
		public SerializableType ListType => listType;

		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Strict)]
		[SerializeField]
		private bool result = false;

		private const string LIST = "list";

		protected override void Init() {
			listType.OnChanged -= RebuildPorts;
			listType.OnChanged += RebuildPorts;
		}

		protected virtual void RebuildPorts() {

			if (listType.Type == null) {
				ClearDynamicPorts();
				return;
			}

			var nodePort = this.GetOrAddDynamicInput(listType.TypeAsList, LIST);

			this.ClearDynamicPortsExcept(new[] { nodePort });

		}

		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying && port.fieldName.Equals(nameof(result))) {
				var inputPort = GetInputPort(LIST);
				var inputValue = inputPort.GetInputValue(context);

				Type type = inputValue.GetType();
				var propertyInfo = type.GetProperty("Count");
				var count = (int)propertyInfo.GetValue(inputValue);
				return count > 0;
			}
			return null;
		}
	}
}