
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {

	[NodeWidth(300)]
	[CreateNodeMenu("GameObject/Unity Object")]
	public class UnityObjectNode : Node {

		[HideLabel, SerializeField]
		private Object targetObject;

		private static string TARGET_OBJECT = "Target Object";

		public void SetObject(Object @object) {
			targetObject = @object;
			UpdatePorts();
		}

		public override void UpdatePorts() {

			if (targetObject == null) {
				ClearDynamicPorts();
			}
			else {
				var objectType = targetObject.GetType();

				var outputPort = this.GetOrAddDynamicOutput(objectType, TARGET_OBJECT, sameLine: true, hideLabel: true);
				this.ClearDynamicPortsExcept(outputPort);
			}

			base.UpdatePorts();
		}

		public override object GetValue(INodeContext context, NodePort port) {
			if (port.fieldName.Equals(TARGET_OBJECT)) {
				return targetObject;
			}
			return null;
		}
	}

}
