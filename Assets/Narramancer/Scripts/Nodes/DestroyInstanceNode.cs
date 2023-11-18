using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Narramancer {
	[CreateNodeMenu("Noun/Destroy Instance")]
	public class DestroyInstanceNode : AbstractInstanceInputChainedRunnableNode {

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			var instance = GetInstance(runner.Blackboard);

			if (instance != null) {
				NarramancerSingleton.Instance.StoryInstance.Instances.Remove(instance);

				if ( instance.HasGameObject) {
					Destroy(instance.GameObject);
				}
			}
		}
	}
}