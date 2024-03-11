
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("List/Subtract Elements from List")]
	public class ListSubtractNode : Node, IListTypeNode {

		[SerializeField]
		private SerializableType listType = new SerializableType();
		public SerializableType ListType => listType;

		private static string LIST_A = "List A";
		private static string LIST_B = "List B";
		private static string ELEMENTS = "Elements";

		private static string RESULT = "Result";

		protected override void Init() {
			listType.OnChanged -= UpdatePorts;
			listType.OnChanged += UpdatePorts;
		}

		public override void UpdatePorts() {

			if (listType.Type == null) {

				ClearDynamicPorts();
				return;
			}

			var nodePorts = new List<NodePort>();

			var genericListType = listType.TypeAsList;

			var nodePort = this.GetOrAddDynamicInput(genericListType, LIST_A);
			nodePorts.Add(nodePort);

			nodePort = this.GetOrAddDynamicInput(genericListType, LIST_B);
			nodePorts.Add(nodePort);

			nodePort = this.GetOrAddDynamicInput(listType.Type, ELEMENTS, ConnectionType.Multiple);
			nodePorts.Add(nodePort);

			nodePort = this.GetOrAddDynamicOutput(genericListType, RESULT);
			nodePorts.Add(nodePort);

			this.ClearDynamicPortsExcept(nodePorts);

			base.UpdatePorts();
		}

		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying && port.fieldName.Equals(RESULT)) {

				ICollection<object> resultList = new List<object>();

				var inputPortA = GetInputPort(LIST_A);
				var inputValueA = inputPortA.GetInputValue(context);

				var inputArrayA = AssemblyUtilities.ToListOfObjects(inputValueA);
				foreach (var element in inputArrayA) {
					resultList.Add(element);
				}


				var inputPortB = GetInputPort(LIST_B);
				if (inputPortB.IsConnected) {

					var inputValueB = inputPortB.GetInputValue(context);
					if (inputValueB != null) {
						var inputArrayB = AssemblyUtilities.ToListOfObjects(inputValueB);

						foreach (var element in inputArrayB) {
							resultList.Remove(element);
						}
					}
				}

				var elementsPort = GetInputPort(ELEMENTS);
				if (elementsPort.IsConnected) {

					var inputElements = elementsPort.GetInputValues(context);
					if (inputElements != null) {
						foreach (var element in inputElements) {
							resultList.Remove(element);
						}
					}
				}

				return resultList;
			}
			return null;
		}
	}
}