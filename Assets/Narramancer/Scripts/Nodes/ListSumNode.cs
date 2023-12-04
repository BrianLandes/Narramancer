
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {

	[NodeWidth(250)]
	public class ListSumNode : Node {

		[SerializeField]
		private SerializableType listType = new SerializableType();


		[SerializeField]
		[VerbRequired, GraphHideLabel]
		[RequireInputFromSerializableType(nameof(listType), "element")]
		[RequireOutput(typeof(float), "value")]
		private ValueVerb predicate = default;

		private const string INPUT_ELEMENTS = "Elements";
		private const string INPUT_LIST = "Input List";

		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Strict)]
		[SerializeField]
		private float sum = 0f;

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

			var inputListPort = this.GetOrAddDynamicInput(listType.TypeAsList, INPUT_LIST);
			existingPorts.Add(inputListPort);

			var inputPort = this.GetOrAddDynamicInput(listType.Type, INPUT_ELEMENTS, ConnectionType.Multiple);
			existingPorts.Add(inputPort);

			foreach (var inputGraphPort in predicate.Inputs) {
				if (inputGraphPort.Type == null) {
					continue;
				}
				// don't add an input port for the Character input -> we'll assign that one manually
				if (inputGraphPort == baseInputPort) {
					continue;
				}
				var nodePort = this.GetOrAddDynamicInput(inputGraphPort.Type, inputGraphPort.Name);
				existingPorts.Add(nodePort);
			}

			this.ClearDynamicPortsExcept(existingPorts);

			base.UpdatePorts();
		}

		private void AssignGraphVariableInputs(object context) {
			foreach (var inputPort in DynamicInputs) {
				var verbPort = predicate.GetInput(inputPort.ValueType, inputPort.fieldName);
				verbPort?.AssignValueFromNodePort(context, inputPort);
			}
		}

		private float GetValueFromElement(object context, object element) {
			AssignGraphVariableInputs(context);
			return predicate.RunForValue<float>(context, listType.Type, element);
		}

		public override object GetValue(object context, NodePort port) {

			if (Application.isPlaying) {
				if (predicate == null) {
					Debug.LogError($"{nameof(ListSumNode)} requires a predicate graph assigned.", this);
					return null;
				}
				switch (port.fieldName) {
					case nameof(sum):

						var inputList = new List<object>();

						var inputListPort = GetInputPort(INPUT_LIST);
						var value = inputListPort.GetInputValue(context);
						if (value != null) {
							inputList.AddRange(AssemblyUtilities.ToListOfObjects(value));
						}

						var inputElementPort = GetInputPort(INPUT_ELEMENTS);
						var inputElements = inputElementPort.GetInputValues(context);
						if (inputElements != null) {
							inputList.AddRange(inputElements);
						}

						var result = 0f;
						foreach (var element in inputList) {
							result += GetValueFromElement(context, element);
						}

						return result;
				}
			}

			return null;
		}
	}
}