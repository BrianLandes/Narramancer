using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("Adjective/Get Property")]
	public class GetPropertyNode : AbstractInstanceInputNode {

		//TODO: allow PropertyNouns AND/OR PropertyInstances

		[Input(backingValue = ShowBackingValue.Unconnected, connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField, HideLabel]
		public PropertyScriptableObject property = default;

		[Output(backingValue = ShowBackingValue.Never, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		private bool hasProperty = false;

		[Output(backingValue = ShowBackingValue.Never, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		private PropertyInstance propertyInstance = default;


		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying) {
				switch (port.fieldName) {
					case nameof(hasProperty): {
							var inputInstance = GetInstance(context);
							Assert.IsNotNull(inputInstance);
							var inputProperty = GetInputValue(context, nameof(property), property);
							Assert.IsNotNull(inputProperty);

							return inputInstance.HasProperty(inputProperty);
						}

					case nameof(propertyInstance): {
							var inputInstance = GetInstance(context);
							Assert.IsNotNull(inputInstance);
							var inputProperty = GetInputValue(context, nameof(property), property);
							Assert.IsNotNull(inputProperty);

							return inputInstance.GetProperty(inputProperty);
						}
				}

			}
			return base.GetValue(context, port);
		}
	}

}
