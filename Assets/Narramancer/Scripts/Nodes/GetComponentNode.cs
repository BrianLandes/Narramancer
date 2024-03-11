using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("GameObject/Get Component")]
	public class GetComponentNode : Node {


		[SerializeField]
		[Input(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Inherited)]
		private GameObject inputGameObject = default;

		[SerializeField]
		private SerializableType listType = new SerializableType();

		[SerializeField]
		private bool includeChildren = false;

		protected override void Init() {
			listType.OnChanged -= UpdatePorts;
			listType.OnChanged += UpdatePorts;
		}

		public override void UpdatePorts() {

			if (listType.Type == null) {
				ClearDynamicPorts();
			}
			else {
				var nodePort = this.GetOrAddDynamicOutput(listType.Type, "component");
				this.ClearDynamicPortsExcept(nodePort);
			}

			base.UpdatePorts();
		}

		public override object GetValue(INodeContext context, NodePort port) {

			if (Application.isPlaying) {
				if (listType.Type == null || !typeof(Component).IsAssignableFrom(listType.Type)) {
					Debug.LogError("Type not set", this);
					return null;
				}
				var inputGameObject = GetInputValue(context, nameof(this.inputGameObject), this.inputGameObject);
				if (inputGameObject == null) {
					Debug.LogError("No gameobject connected", this);
					return null;
				}
				var component = inputGameObject.GetComponent(listType.Type);
				if (component == null && includeChildren) {
					component = inputGameObject.GetComponentInChildren(listType.Type);
				}
				return component;
			}

			return null;
		}
	}
}