

using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {

	public class ChooseObjectNode : Node {

		[SerializeField]
		private SerializableType objectType = new SerializableType();

		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField]
		private bool condition = true;

		private const string INPUT_WHEN_TRUE = "When True";
		private const string INPUT_WHEN_FALSE = "When False";
		private const string RESULT = "Result";

		protected override void Init() {
			objectType.OnChanged -= UpdatePorts;
			objectType.OnChanged += UpdatePorts;
		}

		public override void UpdatePorts() {

			if (objectType.Type == null) {
				ClearDynamicPorts();
			}
			else {
				List<NodePort> existingPorts = new List<NodePort>();

				var type = objectType.Type;

				var inputListPort = this.GetOrAddDynamicInput(type, INPUT_WHEN_TRUE);
				existingPorts.Add(inputListPort);

				inputListPort = this.GetOrAddDynamicInput(type, INPUT_WHEN_FALSE);
				existingPorts.Add(inputListPort);

				var outputListPort = this.GetOrAddDynamicOutput(type, RESULT);
				existingPorts.Add(outputListPort);

				this.ClearDynamicPortsExcept(existingPorts);

			}

			base.UpdatePorts();
		}

		public override object GetValue(INodeContext context, NodePort port) {
			if (port.fieldName.Equals(RESULT)) {

				bool inputCondition = GetInputValue(context, nameof(condition), condition);

				if (inputCondition) {
					var inputValue = this.GetInputValue(context, INPUT_WHEN_TRUE);
					return inputValue;
				}
				else {
					var inputValue = this.GetInputValue(context, INPUT_WHEN_FALSE);
					return inputValue;
				}

			}
			return null;
		}
	}
}
