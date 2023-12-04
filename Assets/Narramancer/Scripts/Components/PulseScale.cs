using UnityEngine;

namespace Narramancer {
	public class PulseScale : MonoBehaviour {

		[SerializeField, Min(1f)]
		float maxScale = 1.3f;

		[SerializeField]
		float pulseSpeed = 3f;

		float time = 0f;

		private void OnEnable() {
			time = 0f;
		}

		private void Update() {
			var scale = 1f + (maxScale - 1f) * (Mathf.Sin(time * pulseSpeed) + 1f) * 0.5f;
			transform.localScale = Vector3.one * scale;
			time += Time.deltaTime;

		}

	}
}