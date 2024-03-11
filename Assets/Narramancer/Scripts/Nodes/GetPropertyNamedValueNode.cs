
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("Adjective/Get Property Named Value")]
	public class GetPropertyNamedValueNode : Node {

		[Input(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField]
		private PropertyInstance property = null;

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
				var property = GetInputValue(context, nameof(this.property), this.property);
				var value = property.Adjective.GetNamedValue(key);
				return value;
			}
			return null;
		}
	}
}