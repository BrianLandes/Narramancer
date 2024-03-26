using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("Variable/Get Instance Variable")]
	public class GetInstanceBlackboardVariableNode : AbstractInstanceInputNode {

		[SerializeField]
		private SerializableType valueType = new SerializableType();

		[SerializeField]
		[Input(ShowBackingValue.Unconnected,ConnectionType.Override, TypeConstraint.Inherited)]
		string key = "value";

		protected override void Init() {
			valueType.OnChanged -= RebuildPorts;
			valueType.OnChanged += RebuildPorts;
		}

		private void RebuildPorts() {

			if (valueType.Type == null) {
				ClearDynamicPorts();
				return;
			}

			var outputValuePort = this.GetOrAddDynamicOutput(valueType.Type, "value", true, true);
			this.ClearDynamicPortsExcept(outputValuePort);

		}


		public override object GetValue(INodeContext context, NodePort port) {

			if (Application.isPlaying && port.fieldName.Equals("value")) {
				var instance = GetInstance(context);
				var key = GetInputValue(context, nameof(this.key), this.key);
				var value = instance?.Blackboard.Get(key, port.ValueType);
				return value;
			}

			return base.GetValue(context, port);
		}
	}
}