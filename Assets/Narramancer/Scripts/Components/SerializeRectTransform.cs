using System;
using UnityEngine;

namespace Narramancer {
	[RequireComponent(typeof(RectTransform))]
	public class SerializeRectTransform : SerializableMonoBehaviour {

		[SerializeMonoBehaviourField]
		private Promise promise;

		[SerializeMonoBehaviourField]
		private bool tweening = false;

		[SerializeMonoBehaviourField]
		private Vector3 targetTweenPosition;

		[SerializeMonoBehaviourField]
		private float speed;

		[Serializable]
		public class SerializedRectTransform {
			public Vector2 anchoredPosition;
		}

		public Promise TweenTo(Vector3 position, float speed) {

			tweening = true;
			targetTweenPosition = position;
			this.speed = speed;

			promise = new Promise();
			return promise;
		}

		private void Update() {
			if (tweening) {
				var vector = targetTweenPosition - transform.position;
				var moveVector = vector.normalized * speed * Time.deltaTime;
				if (vector==Vector3.zero || moveVector.sqrMagnitude > vector.sqrMagnitude) {
					transform.position = targetTweenPosition;
					tweening = false;
					var promise = this.promise;
					this.promise = null;
					promise.Resolve();
				}
				else {
					transform.position += moveVector;
				}
			}
		}

		public override void Serialize(StoryInstance story) {
			base.Serialize(story);
			var rectTransform = GetComponent<RectTransform>();
			var serializedTransform = new SerializedRectTransform() {
				anchoredPosition = rectTransform.anchoredPosition,
			};
			story.Blackboard.Set(Key("transform"), serializedTransform);
		}

		public override void Deserialize(StoryInstance story) {
			base.Deserialize(story);
			var serializedTransform = story.Blackboard.Get<SerializedRectTransform>(Key("transform"));
			var rectTransform = GetComponent<RectTransform>();
			rectTransform.anchoredPosition = serializedTransform.anchoredPosition;
		}
	}
}