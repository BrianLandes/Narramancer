using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("Adjective/Get Property Modifier")]
	public class GetPropertyModifierNode : Node {

		//TODO: allow PropertyNouns AND/OR PropertyInstances

		[Input( ShowBackingValue.Never,ConnectionType.Override,TypeConstraint.Inherited)]
		[SerializeField, HideLabel]
		public PropertyInstance property = default;

		[Output(backingValue = ShowBackingValue.Never, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		private bool hasModifier = false;


		[SerializeField]
		private SerializableType type = new SerializableType();

		private const string MODIFIER = "Modifier";

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
				var outputPort = this.GetOrAddDynamicOutput(type.Type, MODIFIER, sameLine: false, hideLabel: false);
				this.ClearDynamicPortsExcept(outputPort);
			}

			base.UpdatePorts();
		}


		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying) {
				if (type.Type == null) {
					Debug.LogError("Type was null", this);
					return null;
				}
				switch (port.fieldName) {
					case nameof(hasModifier): {

							var inputProperty = GetInputValue(context, nameof(property), property);
							if (inputProperty == null) {
								Debug.LogError("inputProperty was null", this);
								return null;
							}
							return inputProperty.HasModifier(type.Type);
						}

					case MODIFIER: {

							var inputProperty = GetInputValue(context, nameof(property), property);
							if (inputProperty == null) {
								Debug.LogError("inputProperty was null", this);
								return null;
							}

							return inputProperty.GetModifier(type.Type);
						}
				}

			}
			return null;
		}
	}

}
