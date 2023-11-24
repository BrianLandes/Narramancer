using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {

	public class ListSelectNode : Node {

		[SerializeField]
		[VerbRequired]
		[GraphHideLabel]
		[RequireInput]
		[RequireOutput]
		private ValueVerb predicate = default;

		private const string INPUT_LIST = "Input List";
		private const string OUTPUT_LIST = "Output List";

		public override void UpdatePorts() {

			var nodePorts = new List<NodePort>();

			if (predicate != null) {
				var firstInput = predicate.Inputs.First();

				if (firstInput != null) {
					var inputListType = typeof(List<>).MakeGenericType(firstInput.Type);
					var inputListPort = this.GetOrAddDynamicInput(inputListType, INPUT_LIST);
					nodePorts.Add(inputListPort);
				}

				var firstOutput = predicate.Outputs.First();
				if (firstOutput != null) {
					var outputListType = typeof(List<>).MakeGenericType(firstOutput.Type);
					var outputListPort = this.GetOrAddDynamicOutput(outputListType, OUTPUT_LIST);
					nodePorts.Add(outputListPort);
				}

				foreach (var inputGraphPort in predicate.Inputs) {
					if (inputGraphPort.Type == null) {
						continue;
					}
					// don't add an input port for the initial input -> we'll assign that one manually
					if (inputGraphPort == firstInput) {
						continue;
					}
					var nodePort = this.GetOrAddDynamicInput(inputGraphPort.Type, inputGraphPort.Name);
					nodePorts.Add(nodePort);
				}
			}

			this.ClearDynamicPortsExcept(nodePorts);

			base.UpdatePorts();
		}

		private void AssignGraphVariableInputs(object context) {
			foreach (var inputPort in DynamicInputs) {
				var verbPort = predicate.GetInput(inputPort.ValueType, inputPort.fieldName);
				verbPort?.AssignValueFromNodePort(context, inputPort);
			}
		}

		public override object GetValue(object context, NodePort port) {
			if (Application.isPlaying && port.fieldName.Equals(OUTPUT_LIST)) {

				if (predicate == null) {
					Debug.LogError($"{GetType().Name} needs a Predicate Graph", this);
					return null;
				}


				AssignGraphVariableInputs(context);

				var resultList = new List<object>();

				var inputPort = GetInputPort(INPUT_LIST);
				var inputArrayA = inputPort.GetInputValueObjectList(context);

				foreach (var element in inputArrayA) {

					var resultElement = predicate.RunForValue<object, object>(context, element);

					resultList.Add(resultElement);
				}

				return resultList;
			}
			return null;
		}
	}
}