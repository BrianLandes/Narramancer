using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("List/First Element from List")]
	public class ListFirstNode : Node, IListTypeNode {

		[SerializeField]
		private SerializableType listType = new SerializableType();
		public SerializableType ListType => listType;

		private const string LIST = "List";

		private const string FIRST_ELEMENT = "First Element";

		protected override void Init() {
			listType.OnChanged -= UpdatePorts;
			listType.OnChanged += UpdatePorts;
		}

		public override void UpdatePorts() {

			if (listType.Type == null) {
				ClearDynamicPorts();
				return;
			}

			var keepPorts = new List<NodePort>();

			var inputPort = this.GetOrAddDynamicInput(listType.TypeAsList, LIST);
			keepPorts.Add(inputPort);

			var outputPort = this.GetOrAddDynamicOutput(listType.Type, FIRST_ELEMENT, sameLine: true);
			keepPorts.Add(outputPort);

			this.ClearDynamicPortsExcept(keepPorts);

			base.UpdatePorts();

		}

		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying && port.fieldName.Equals(FIRST_ELEMENT)) {
				var inputPort = GetInputPort(LIST);

				var inputArray = inputPort.GetInputValueObjectList(context);

				if (inputArray.Count >= 1) {
					return inputArray[0];
				}
			}
			return null;
		}
	}
}