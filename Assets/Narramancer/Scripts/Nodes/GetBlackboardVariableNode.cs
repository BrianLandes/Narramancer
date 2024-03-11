
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("Variable/Get Blackboard Variable")]
	public class GetBlackboardVariableNode : Node {

		[SerializeField]
		private SerializableType valueType = new SerializableType();

		[SerializeField]
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
			if (!Application.isPlaying) {
				return null;
			}

			if (port.fieldName.Equals("value")) {
				var blackboard = context as Blackboard;
				var value = blackboard.Get(key, port.ValueType);
				return value;
			}
			return null;
		}
	}
}