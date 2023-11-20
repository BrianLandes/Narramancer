using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {

	[CreateNodeMenu("Noun/Get Instances")]
	public class GetInstancesNode : Node {

		[Serializable]
		public enum TypeFilter {
			All,
			Characters,
			Items,
			Locations
		}
		[SerializeField]
		[NodeEnum]
		TypeFilter filter = TypeFilter.All;

		[Output]
		[SerializeField]
		[HideLabel]
		private List<NounInstance> instances = default;

		public override object GetValue(object context, NodePort port) {
			if (Application.isPlaying && port.fieldName.Equals(nameof(instances))) {
				switch (filter) {
					case TypeFilter.All:
						return NarramancerSingleton.Instance.GetInstances();

					case TypeFilter.Characters:
						return NarramancerSingleton.Instance.GetInstances().Where(x=>x.NounType == NounType.Character).ToList();

					case TypeFilter.Items:
						return NarramancerSingleton.Instance.GetInstances().Where(x => x.NounType == NounType.Item).ToList();

					case TypeFilter.Locations:
						return NarramancerSingleton.Instance.GetInstances().Where(x => x.NounType == NounType.Location).ToList();
				}
				
			}
			return null;
		}
	}

}
