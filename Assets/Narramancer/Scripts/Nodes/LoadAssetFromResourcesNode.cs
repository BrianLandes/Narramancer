using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer
{
    public class LoadAssetFromResourcesNode : Node
    {

		[SerializeField]
		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		private string path = "";

		[SerializeField]
		private SerializableType type = new SerializableType();

		private static string ASSET = "Asset";

		protected override void Init() {
			base.Init();
			type.OnChanged -= UpdatePorts;
			type.OnChanged += UpdatePorts;
		}

		public override void UpdatePorts() {

			if (type.Type == null) {
				ClearDynamicPorts();
			}
			else {
				var outputPort = this.GetOrAddDynamicOutput(type.Type, ASSET, sameLine: false, hideLabel: false);
				this.ClearDynamicPortsExcept(outputPort);
			}

			base.UpdatePorts();
		}

		public override object GetValue(INodeContext context, NodePort port) {
			if (port.fieldName.Equals(ASSET)) {
				if (type.Type == null) {
					Debug.LogError("Type was null", this);
					return null;
				}
				var path = GetInputValue(context, nameof(this.path), this.path);
				var result = Resources.Load(path, type.Type);
				// TODO: ensure that the result is of the given type
				return result;
			}
			return null;
		}
	}
}
