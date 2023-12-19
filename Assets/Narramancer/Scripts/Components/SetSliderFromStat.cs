using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Narramancer {
	[RequireComponent(typeof(Slider))]
	public class SetSliderFromStat : MonoBehaviour {

		[SerializeField]
		NounScriptableObject noun = default;

		[SerializeField]
		StatScriptableObject stat = default;

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

		Slider GetSlider() {
			return GetComponent<Slider>();
		}

		private void Update() {

			var slider = GetSlider();
			
			var instance = GetInstance();

			if (slider==null || instance == null) {
				return;
			}

			var value = instance.GetStatEffectiveValue(null, stat);
			slider.value = value;
		}
	}
}
