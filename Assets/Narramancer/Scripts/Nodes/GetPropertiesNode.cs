using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("Adjective/Get Properties")]
	public class GetPropertiesNode : AbstractInstanceInputNode {

		[Output(backingValue = ShowBackingValue.Never, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		private List<PropertyInstance> propertyInstances = default;


		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying) {
				switch (port.fieldName) {

					case nameof(propertyInstances): {
							var inputInstance = GetInstance(context);
							if (inputInstance == null) {
								Debug.LogError("Instance was null", this);
								return null;
							}
							return inputInstance.GetAllProperties();
						}
				}

			}
			return base.GetValue(context, port);
		}
	}

}
