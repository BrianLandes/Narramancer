using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Narramancer {

	[CreateNodeMenu("Adjective/Remove Properties")]
	public class RemovePropertiesNode : AbstractInstanceInputChainedRunnableNode {

		public List<PropertyScriptableObject> properties;

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			var instance = GetInstance(runner.Blackboard);
			Assert.IsNotNull(instance);

			instance.RemoveProperties(properties);
		}
	}
}