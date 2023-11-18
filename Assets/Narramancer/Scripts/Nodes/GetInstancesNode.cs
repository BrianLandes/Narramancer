using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {

	[CreateNodeMenu("Noun/Get Instances")]
	public class GetInstancesNode : Node {

		[Output]
		[SerializeField]
		[HideLabel]
		private List<NounInstance> instances = default;

		// TODO: filter by NounType

		public override object GetValue(object context, NodePort port) {
			if (Application.isPlaying && port.fieldName.Equals(nameof(instances))) {
				return NarramancerSingleton.Instance.GetInstances();
			}
			return null;
		}
	}

}
