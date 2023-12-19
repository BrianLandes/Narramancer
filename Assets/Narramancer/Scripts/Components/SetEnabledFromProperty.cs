using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Narramancer {

	public class SetEnabledFromProperty : MonoBehaviour {

		[SerializeField]
		GameObject targetGameObject = default;

		[SerializeField]
		NounScriptableObject noun = default;

		[SerializeField]
		PropertyScriptableObject property = default;

		NounInstance GetInstance() {
			if (noun != null) {
				return NarramancerSingleton.Instance.GetInstance(noun);
			}

			var nounReference = GetComponentInParent<SerializeNounInstanceReference>();
			if (nounReference != null) {
				return nounReference.GetInstance();
			}

			return null;
		}

		private void Update() {

			var instance = GetInstance();

			if (targetGameObject == null || instance == null) {
				return;
			}

			var hasProperty = instance.HasProperty(property);
			targetGameObject.SetActive(hasProperty);
		}
	}
}
