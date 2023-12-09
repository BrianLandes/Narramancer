using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {

	[CreateNodeMenu("Noun/Get Instances")]
	public class GetInstancesNode : Node {

		[SerializeField]
		List<PropertyScriptableObject> mustHaveProperties = new List<PropertyScriptableObject>();

		[Output]
		[SerializeField]
		[HideLabel]
		private List<NounInstance> instances = default;

		public override object GetValue(object context, NodePort port) {
			if (Application.isPlaying && port.fieldName.Equals(nameof(instances))) {

				var resultList = NarramancerSingleton.Instance.GetInstances();

				if (mustHaveProperties.Any()) {
					resultList = resultList.Where(x => mustHaveProperties.All(property => x.HasProperty(property))).ToList();
				}

				return resultList;
			}
			return null;
		}
	}

}
