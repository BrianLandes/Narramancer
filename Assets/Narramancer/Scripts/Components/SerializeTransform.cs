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
		private Promise positionPromise;

		[SerializeMonoBehaviourField]
		private bool tweeningPosition = false;

		[SerializeMonoBehaviourField]
		private Vector3 targetTweenPosition;

		[SerializeMonoBehaviourField]
		private float positionSpeed;

		[SerializeMonoBehaviourField]
		private Promise rotationPromise;

		[SerializeMonoBehaviourField]
		private bool tweeningRotation = false;

		[SerializeMonoBehaviourField]
		private Quaternion targetTweenRotation;

		[SerializeMonoBehaviourField]
		private float rotationSpeed;

		public Promise TweenTo(Vector3 position, float speed) {

			tweeningPosition = true;
			targetTweenPosition = position;
			this.positionSpeed = speed;

			positionPromise = new Promise();
			return positionPromise;
		}

		public Promise TweenTo(Quaternion rotation, float speed) {

			tweeningRotation = true;
			targetTweenRotation = rotation;
			this.rotationSpeed = speed;

			rotationPromise = new Promise();
			return rotationPromise;
		}

		private void Update() {
			if (tweeningPosition) {
				var vector = targetTweenPosition - transform.position;
				var moveVector = vector.normalized * positionSpeed * Time.deltaTime;
				if (moveVector.sqrMagnitude > vector.sqrMagnitude) {
					transform.position = targetTweenPosition;
					tweeningPosition = false;
					var promise = this.positionPromise;
					this.positionPromise = null;
					promise.Resolve();
				}
				else {
					transform.position += moveVector;
				}
			}
			if (tweeningRotation) {
				var dot = Quaternion.Dot(targetTweenRotation, transform.rotation);
				if (Mathf.Approximately(0, dot)) {
					tweeningRotation = false;
					var promise = this.rotationPromise;
					this.rotationPromise = null;
					promise.Resolve();
				}
				else {
					var targetRotation = Quaternion.Lerp(transform.rotation, targetTweenRotation, rotationSpeed * Time.deltaTime);
					transform.rotation = targetRotation;
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