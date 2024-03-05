﻿
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {

	[NodeWidth(250)]
	public class ListOrderNode : Node {

		[SerializeField]
		private SerializableType listType = new SerializableType();


		[SerializeField]
		[HideLabelInNode, VerbRequired]
		[RequireInputFromSerializableType(nameof(listType), "element")]
		[RequireOutput(typeof(float), "value")]
		private ValueVerb predicate = default;

		[SerializeField]
		bool descending = true;

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
			var baseOutputPort = predicate.GetOutput<float>();

			var inputListPort = this.GetOrAddDynamicInput(listType.TypeAsList, INPUT_LIST);
			existingPorts.Add(inputListPort);

			var outputListPort = this.GetOrAddDynamicOutput(listType.TypeAsList, OUTPUT_LIST, sameLine: true);
			existingPorts.Add(outputListPort);

			var miscPorts = this.GetOrAddPortsForGraph(predicate, new[] { baseInputPort, baseOutputPort });
			existingPorts.AddRange(miscPorts);

			this.ClearDynamicPortsExcept(existingPorts);

			base.UpdatePorts();
		}

		private void AssignGraphVariableInputs(IDictionary<string, object> context) {
			foreach (var inputPort in DynamicInputs) {
				var verbPort = predicate.GetInput(inputPort.ValueType, inputPort.fieldName);
				verbPort?.AssignValueFromNodePort(context, inputPort);
			}
		}

		private float GetElementRankValue(IDictionary<string, object> context, object element) {

			AssignGraphVariableInputs(context);

			return predicate.RunForValue<float>(context, listType.Type, element);
		}

		private List<object> GetOrderedElements(IDictionary<string, object> context) {
			var inputListPort = GetInputPort(INPUT_LIST);
			var inputArray = inputListPort.GetInputValueObjectList(context);
			if (descending) {
				var resultList = inputArray.OrderByDescending(element => GetElementRankValue(context, element)).ToList();
				return resultList;
			}
			else {
				var resultList = inputArray.OrderBy(element => GetElementRankValue(context, element)).ToList();
				return resultList;
			}
		}

		public override object GetValue(IDictionary<string, object> context, NodePort port) {

			if (Application.isPlaying) {
				if (predicate == null) {
					Debug.LogError($"{nameof(ListFilterNode)} must have a valid predicate graph assigned.", this);
					return null;
				}
				switch (port.fieldName) {
					case OUTPUT_LIST:
						var elements = GetOrderedElements(context);
						return elements;
				}
			}

			return null;
		}
	}
}