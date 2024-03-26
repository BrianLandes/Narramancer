using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("GameObject/Get Component")]
	public class GetComponentNode : Node {


		[SerializeField]
		[Input(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Inherited)]
		private GameObject inputGameObject = default;

		[SerializeField]
		[FormerlySerializedAs("listType")]
		private SerializableType componentType = new SerializableType();

		[SerializeField]
		private bool includeChildren = false;

		protected override void Init() {
			componentType.OnChanged -= UpdatePorts;
			componentType.OnChanged += UpdatePorts;

			componentType.typeFilter = (type) => typeof(Component).IsAssignableFrom(type);
			componentType.canBeList = false;
		}

		public override void UpdatePorts() {

			if (componentType.Type == null) {
				ClearDynamicPorts();
			}
			else {
				var nodePort = this.GetOrAddDynamicOutput(componentType.Type, "component");
				this.ClearDynamicPortsExcept(nodePort);
			}

			base.UpdatePorts();
		}

		public override object GetValue(INodeContext context, NodePort port) {

			if (Application.isPlaying) {
				if (componentType.Type == null || !typeof(Component).IsAssignableFrom(componentType.Type)) {
					Debug.LogError("Type not set", this);
					return null;
				}
				var inputGameObject = GetInputValue(context, nameof(this.inputGameObject), this.inputGameObject);
				if (inputGameObject == null) {
					Debug.LogError("No gameobject connected", this);
					return null;
				}
				var component = inputGameObject.GetComponent(componentType.Type);
				if (component == null && includeChildren) {
					component = inputGameObject.GetComponentInChildren(componentType.Type);
				}
				return component;
			}

			return null;
		}
	}
}