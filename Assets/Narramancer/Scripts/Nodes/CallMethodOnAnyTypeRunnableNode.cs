

using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	public class CallMethodOnAnyTypeRunnableNode : AbstractDynamicMethodRunnableNode {

		[SerializeField]
		private SerializableType type = new SerializableType();

		private const string TARGET = "Target";
		private const string PASS_THROUGH = "Pass Through";

		protected override void Init() {
			base.Init();
			type.OnChanged -= RebuildPorts;
			type.OnChanged += RebuildPorts;
			method.LookupTypes = new[] { type.Type };
		}

		protected override object GetTargetObject(INodeContext context) {
			if (type.Type != null) {
				var existingInputPort = this.GetDynamicInput(type.Type, TARGET);
				if (existingInputPort != null) {
					return existingInputPort.GetInputValue(context);
				}
			}
			return null;
		}

		protected override void RebuildPorts() {

			UpdateNodeName();

			List<NodePort> existingPorts = new List<NodePort>();

			if (type.Type == null || (method.IsValid() && type.Type != method.TargetType)) {
				method.Clear();
			}

			if (method.IsValid()) {
				var inputs = CreateParameterInputs();
				existingPorts.AddRange(inputs);
			}

			if (type.Type != null) {
				method.LookupTypes = new[] { type.Type };

				var targetPort = this.GetOrAddDynamicInput(type.Type, TARGET);
				existingPorts.Add(targetPort);

				var passThroughPort = this.GetOrAddDynamicOutput(type.Type, PASS_THROUGH, true);
				existingPorts.Add(passThroughPort);
			}

			this.ClearDynamicPortsExcept(existingPorts);
		}


		public override object GetValue(INodeContext context, NodePort port) {

			if (port.fieldName.Equals(PASS_THROUGH)) {
				var targetPort = GetInputPort(TARGET);
				return targetPort.GetInputValue(context);
			}

			return base.GetValue(context, port);
		}

	}
}