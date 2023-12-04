using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Narramancer {

	[CreateNodeMenu("Adjective/Add Property or Properties")]
	public class AddPropertiesNode : AbstractInstanceInputChainedRunnableNode {

		[SerializeField]
		private List<PropertyScriptableObject> properties = default;

		[SerializeField]
		[Input(ShowBackingValue.Unconnected, ConnectionType.Multiple, TypeConstraint.Inherited)]
		private PropertyScriptableObject property = default;

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			var instance = GetInstance(runner.Blackboard);
			Assert.IsNotNull(instance);

			instance.AddProperties(properties);

			var inputProperties = GetInputValues(runner.Blackboard, nameof(this.property), this.property);
			if (inputProperties != null) {
				instance.AddProperties(inputProperties);
			}
		}
	}
}