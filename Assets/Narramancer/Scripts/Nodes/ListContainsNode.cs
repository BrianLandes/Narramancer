
using System;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("List/List Contains Element")]
	public class ListContainsNode : Node, IListTypeNode {

		[SerializeField]
		private SerializableType listType = new SerializableType();
		public SerializableType ListType => listType;

		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Strict)]
		[SerializeField]
		private bool result = false;

		private const string LIST = "list";
		private const string ELEMENT = "element";

		protected override void Init() {
			listType.OnChanged -= RebuildPorts;
			listType.OnChanged += RebuildPorts;
		}

		protected virtual void RebuildPorts() {

			if (listType.Type == null) {
				ClearDynamicPorts();
				return;
			}

			var listPort = this.GetOrAddDynamicInput(listType.TypeAsList, LIST);

			var elementPort = this.GetOrAddDynamicInput(listType.Type, ELEMENT);

			this.ClearDynamicPortsExcept(new[] { listPort, elementPort });

		}

		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying && port.fieldName.Equals(nameof(result))) {
				var inputPort = GetInputPort(LIST);
				var inputList = inputPort.GetInputValueObjectList(context);

				var element = GetInputValue<object>(context, ELEMENT);

				var contains = inputList.Contains(element);
				return contains;
			}
			return null;
		}
	}
}