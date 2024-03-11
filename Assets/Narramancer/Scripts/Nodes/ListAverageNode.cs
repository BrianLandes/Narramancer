﻿
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {

	[NodeWidth(250)]
	public class ListAverageNode : Node {

		[SerializeField]
		private SerializableType listType = new SerializableType();


		[SerializeField]
		[VerbRequired]
		[HideLabelInNode]
		[RequireInputFromSerializableType(nameof(listType), "element")]
		[RequireOutput(typeof(float), "average")]
		public ValueVerb predicate = default;

		private const string INPUT_LIST = "Input List";

		[SerializeField]
		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Strict)]
		private float average = 0f;

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

		private void AssignGraphVariableInputs(INodeContext context) {
			foreach (var inputPort in DynamicInputs) {
				var verbPort = predicate.GetInput(inputPort.ValueType, inputPort.fieldName);
				verbPort?.AssignValueFromNodePort(context, inputPort);
			}
		}

		public override object GetValue(INodeContext context, NodePort port) {

			if (Application.isPlaying) {
				if (predicate == null) {
					Debug.Log($"{nameof(ListAverageNode)} must have a predicate graph assigned.", this);
					return null;
				}
				switch (port.fieldName) {
					case nameof(average):
						var inputListPort = GetInputPort(INPUT_LIST);
						var inputList = inputListPort.GetInputValueObjectList(context);

						if (!inputList.Any()) {
							return 0f;
						}

						var type = listType.Type;

						var result = 0f;
						foreach (var element in inputList) {
							AssignGraphVariableInputs(context);
							var value = predicate.RunForValue<float>(context, type, element);
							result += value;
						}

						return result / inputList.Count;
				}
			}

			return null;
		}
	}
}