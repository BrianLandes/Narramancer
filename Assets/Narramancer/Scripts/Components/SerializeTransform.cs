using System;
using UnityEngine;

namespace Narramancer {
	public class SerializeTransform : SerializableMonoBehaviour {

		[Serializable]
		public class SerializedTransform {
			public Vector3 position;
			public Quaternion rotation;
			public Vector3 localScale;
		}


		[SerializeMonoBehaviourField]
		private Promise promise;

		[SerializeMonoBehaviourField]
		private bool tweening = false;

		[SerializeMonoBehaviourField]
		private Vector3 targetTweenPosition;

		[SerializeMonoBehaviourField]
		private float speed;

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
				if (moveVector.sqrMagnitude > vector.sqrMagnitude) {
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
			var serializedTransform = new SerializedTransform() {
				position = transform.position,
				rotation = transform.rotation,
				localScale = transform.localScale
			};
			story.SaveTable.Set(Key("transform"), serializedTransform);
		}

		public override void Deserialize(StoryInstance story) {
			base.Deserialize(story);
			var serializedTransform = story.SaveTable.GetAndRemove<SerializedTransform>(Key("transform"));
			transform.position = serializedTransform.position;
			transform.rotation = serializedTransform.rotation;
			transform.localScale = serializedTransform.localScale;
		}
	}
}