
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {

	[CreateNodeMenu("List/Union of Lists")]
	public class ListUnionNode : Node, IListTypeNode {

		[SerializeField]
		private SerializableType listType = new SerializableType();
		public SerializableType ListType => listType;

		[SerializeField]
		private bool useReplacement = true;

		const string LIST_A = "List A";
		const string LIST_B = "List B";
		const string ELEMENTS = "Elements";
		const string RESULTS = "Results";

		protected override void Init() {
			listType.OnChanged -= UpdatePorts;
			listType.OnChanged += UpdatePorts;
		}

		public override void UpdatePorts() {
			if (listType.Type == null) {
				ClearDynamicPorts();
			}
			else {
				var ports = new List<NodePort>();

				var genericListType = listType.TypeAsList;

				var listAPort = this.GetOrAddDynamicInput(genericListType, LIST_A);
				ports.Add(listAPort);

				var listBPort = this.GetOrAddDynamicInput(genericListType, LIST_B);
				ports.Add(listBPort);

				var elementsPort = this.GetOrAddDynamicInput(listType.Type, ELEMENTS, ConnectionType.Multiple);
				ports.Add(elementsPort);

				var resultPort = this.GetOrAddDynamicOutput(genericListType, RESULTS);
				ports.Add(resultPort);

				this.ClearDynamicPortsExcept(ports);
			}

			base.UpdatePorts();

		}

		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying && port.fieldName.Equals(RESULTS)) {

				ICollection<object> resultList = new List<object>();

				if (useReplacement) {
					resultList = new HashSet<object>();
				}

				var inputPortA = GetInputPort(LIST_A);

				if (inputPortA.IsConnected) {
					var inputValueA = inputPortA.GetInputValue(context);

					var inputArrayA = AssemblyUtilities.ToListOfObjects(inputValueA);
					foreach (var element in inputArrayA.WithoutNulls()) {
						resultList.Add(element);
					}

				}


				var inputPortB = GetInputPort(LIST_B);
				if (inputPortB.IsConnected) {

					var inputValueB = inputPortB.GetInputValue(context);
					if (inputValueB != null) {
						var inputArrayB = AssemblyUtilities.ToListOfObjects(inputValueB);

						foreach (var element in inputArrayB.WithoutNulls()) {
							resultList.Add(element);
						}
					}
				}

				var elementsPort = GetInputPort(ELEMENTS);
				if (elementsPort.IsConnected) {

					var inputElements = elementsPort.GetInputValues(context);
					if (inputElements != null) {
						foreach (var element in inputElements.WithoutNulls()) {
							resultList.Add(element);
						}
					}
				}

				if (useReplacement) {
					resultList = new List<object>(resultList);
				}

				return resultList;
			}
			return null;
		}
	}
}