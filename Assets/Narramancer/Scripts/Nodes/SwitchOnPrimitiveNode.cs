

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {

	public class SwitchOnPrimitiveNode : ResizableNode {

		[SerializeField]
		private SerializableType inputType = new SerializableType();
		public static string InputTypeFieldName => nameof(inputType);

		[SerializeField]
		private SerializableType outputType = new SerializableType();
		public static string OutputTypeFieldName => nameof(outputType);

		public const string INPUT_ELEMENT = "Input Element";
		public const string DEFAULT_OUTPUT_VALUE = "Default Output Value";
		public const string OUTPUT_ELEMENT = "Output Element";

		[SerializeField]
		private PrimitivePrimitiveDictionary pairings = new PrimitivePrimitiveDictionary();
		public static string PairingsFieldName => nameof(pairings);


		protected override void Init() {
			inputType.OnChanged -= UpdatePorts;
			inputType.OnChanged += UpdatePorts;

			outputType.OnChanged -= UpdatePorts;
			outputType.OnChanged += UpdatePorts;
		}

		public override void UpdatePorts() {

			var existingPorts = new List<NodePort>();

			if (inputType.Type != null) {

				var inputListPort = this.GetOrAddDynamicInput(inputType.Type, INPUT_ELEMENT);
				existingPorts.Add(inputListPort);

			}

			if (outputType.Type != null) {

				var outputListPort = this.GetOrAddDynamicOutput(outputType.Type, OUTPUT_ELEMENT);
				existingPorts.Add(outputListPort);

				var defaultValuePort = this.GetOrAddDynamicInput(outputType.Type, DEFAULT_OUTPUT_VALUE);
				existingPorts.Add(defaultValuePort);

			}

			this.ClearDynamicPortsExcept(existingPorts);

			base.UpdatePorts();
		}

		public override object GetValue(INodeContext context, NodePort port) {
			if (port.fieldName.Equals(OUTPUT_ELEMENT)) {
				var inputPort = GetInputPort(INPUT_ELEMENT);
				var inputValue = inputPort.GetInputValue(context);

				foreach (var pairing in pairings) {
					var key = pairing.Key.GetValue();
					if (key == inputValue) {
						//var inputValue = this.GetInputValue(context, pairing.portName);
						return pairing.Value.GetValue();
					}
				}

				var defaultValuePort = GetInputPort(DEFAULT_OUTPUT_VALUE);
				var defaultValue = defaultValuePort.GetInputValue(context);

				return defaultValue;
			}
			return null;
		}

		public (SerializablePrimitive, SerializablePrimitive) AddNewPairing() {
			var keyParameter = new SerializablePrimitive();
			keyParameter.SetType(inputType.Type);
			var elementParameter = new SerializablePrimitive();
			elementParameter.SetType(outputType.Type);
			pairings[keyParameter] = elementParameter;

			//var newPort = this.AddDynamicInput(typeof(Sprite), ConnectionType.Override, TypeConstraint.Inherited);
			//newPairing.portName = newPort.fieldName;

			return (keyParameter, elementParameter);
		}

		public void RemovePairing(int index) {
			var key = pairings.Keys.ElementAt(index);
			pairings.Remove(key);
		}

	}
}
