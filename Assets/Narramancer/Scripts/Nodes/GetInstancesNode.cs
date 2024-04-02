using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {

	[CreateNodeMenu("Noun/Get Instances")]
	[NodeSearchTerms("Get All Instances")]
	public class GetInstancesNode : Node {

		[SerializeField]
		List<PropertyScriptableObject> mustHaveProperties = new List<PropertyScriptableObject>();

		[SerializeField]
		List<PropertyScriptableObject> mustNotHaveProperties = new List<PropertyScriptableObject>();

		[Output]
		[SerializeField]
		[HideLabel]
		private List<NounInstance> instances = default;

		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying && port.fieldName.Equals(nameof(instances))) {

				var query = new NounInstancesQuery() {
					mustHaveProperties = mustHaveProperties.ToArray(),
					mustNotHaveProperties = mustNotHaveProperties.ToArray(),
				};

				var resultList = NarramancerSingleton.Instance.GetInstances(query);

				return resultList;
			}
			return null;
		}
	}

}
