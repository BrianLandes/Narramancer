
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {

	[NodeWidth(250)]
	public class ListFilterNode : Node {

		[SerializeField]
		private SerializableType listType = new SerializableType();


		[SerializeField]
		[GraphHideLabel, GraphRequired]
		[RequireInputFromSerializableType(nameof(listType), "element")]
		[RequireOutput(typeof(bool), "result")]
		private ValueVerb predicate = default;

		private const string INPUT_LIST = "Input List";
		private const string OUTPUT_LIST = "Output List";

		protected override void Init() {
			listType.OnChanged -= UpdatePorts;
			listType.OnChanged += UpdatePorts;
		}

		public override void UpdatePorts() {

			if (predicate == null || listType.Type == null) {
				ClearDynamicPorts();
				return;
			}

			List<NodePort> existingPorts = new List<NodePort>();

			var baseInputPort = predicate.GetInput(listType.Type);
			var baseOutputPort = predicate.GetOutput<bool>();

			var inputListPort = this.GetOrAddDynamicInput(listType.TypeAsList, INPUT_LIST);
			existingPorts.Add(inputListPort);

			var outputListPort = this.GetOrAddDynamicOutput(listType.TypeAsList, OUTPUT_LIST);
			existingPorts.Add(outputListPort);

			var miscPorts = this.GetOrAddPortsForGraph(predicate, new[] { baseInputPort, baseOutputPort });
			existingPorts.AddRange(miscPorts);

			this.ClearDynamicPortsExcept(existingPorts);

			base.UpdatePorts();
		}

		private void AssignGraphVariableInputs(object context) {
			foreach (var inputPort in DynamicInputs) {
				var verbPort = predicate.GetInput(inputPort.ValueType, inputPort.fieldName);
				verbPort?.AssignValueFromNodePort(context, inputPort);
			}
		}

		private bool DoesElementPassPredicate(object context, object element) {

			AssignGraphVariableInputs(context);

			return predicate.RunForValue<bool>(context, listType.Type, element);
		}

		private List<object> GetFilteredElements(object context) {
			var inputListPort = GetInputPort(INPUT_LIST);
			var inputArray = inputListPort.GetInputValueObjectList(context);

			var resultList = new List<object>();
			foreach (var element in inputArray) {
				if (DoesElementPassPredicate(context, element)) {
					resultList.Add(element);
				}
			}

			return resultList;
		}

		public override object GetValue(object context, NodePort port) {

			if (Application.isPlaying) {
				if (predicate == null) {
					Debug.LogError($"{nameof(ListFilterNode)} must have a valid predicate graph assigned.", this);
					return null;
				}
				switch (port.fieldName) {
					case OUTPUT_LIST:
						var elements = GetFilteredElements(context);
						return elements;
				}
			}

			return null;
		}
	}
}