using UnityEngine;

namespace Narramancer {

	/// <summary>
	/// Moves and turns its object over time, and survives a save part-way through. Added on demand by
	/// <see cref="TranslateGameObjectNode"/> and <see cref="TweenGameObjectRotationNode"/>, so an object no longer
	/// needs a <c>SerializeTransform</c> placed on it in advance to be tweened.
	/// <para>
	/// Its fields are <see cref="SaveAttribute"/>s, saved by the object's <see cref="Saveable"/> — which also
	/// re-adds this component on load, since one a node added mid-story is not in the scene file.
	/// </para>
	/// </summary>
	public class Tweener : MonoBehaviour {

		[Save]
		private Promise positionPromise;

		[Save]
		private bool tweeningPosition = false;

		[Save]
		private Vector3 targetPosition;

		[Save]
		private float positionSpeed;

		[Save]
		private Promise rotationPromise;

		[Save]
		private bool tweeningRotation = false;

		[Save]
		private Quaternion targetRotation;

		[Save]
		private float rotationSpeed;

		/// <summary>The object's Tweener, adding one if it has none.</summary>
		public static Tweener On(GameObject gameObject) {
			if (!gameObject.TryGetComponent<Tweener>(out var tweener)) {
				tweener = gameObject.AddComponent<Tweener>();
				if (!gameObject.TryGetComponent<Saveable>(out _)) {
					Debug.LogWarning($"'{gameObject.name}' has no {nameof(Saveable)}, so a save made while it is moving will not resume the move.", gameObject);
				}
			}
			return tweener;
		}

		/// <summary>Moves to <paramref name="position"/> at <paramref name="speed"/> units per second.</summary>
		public Promise MoveTo(Vector3 position, float speed) {
			// A tween replaced mid-flight resolves its promise, or whatever was waiting on it would wait forever.
			positionPromise?.Resolve();
			tweeningPosition = true;
			targetPosition = position;
			positionSpeed = speed;
			positionPromise = new Promise();
			return positionPromise;
		}

		/// <summary>Turns toward <paramref name="rotation"/>, closing <paramref name="speed"/> of the remaining gap each second.</summary>
		public Promise RotateTo(Quaternion rotation, float speed) {
			rotationPromise?.Resolve();
			tweeningRotation = true;
			targetRotation = rotation;
			rotationSpeed = speed;
			rotationPromise = new Promise();
			return rotationPromise;
		}

		public bool IsMoving => tweeningPosition;

		public bool IsRotating => tweeningRotation;

		private void Update() {
			Step(Time.deltaTime);
		}

		/// <summary>Advances both tweens by <paramref name="deltaTime"/>. Public so a test can drive it without a frame loop.</summary>
		public void Step(float deltaTime) {
			if (tweeningPosition) {
				var remaining = targetPosition - transform.position;
				var move = remaining.normalized * positionSpeed * deltaTime;
				if (remaining == Vector3.zero || move.sqrMagnitude >= remaining.sqrMagnitude) {
					transform.position = targetPosition;
					tweeningPosition = false;
					Resolve(ref positionPromise);
				}
				else {
					transform.position += move;
				}
			}
			if (tweeningRotation) {
				var rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotationSpeed * deltaTime);
				// The old check was Mathf.Approximately(0, Quaternion.Dot(...)), which is true when the two are 90
				// degrees *apart* — so a rotation tween essentially never finished, and a verb waiting on it hung.
				if (Quaternion.Angle(rotation, targetRotation) < 0.1f) {
					transform.rotation = targetRotation;
					tweeningRotation = false;
					Resolve(ref rotationPromise);
				}
				else {
					transform.rotation = rotation;
				}
			}
		}

		private static void Resolve(ref Promise promise) {
			var resolved = promise;
			promise = null;
			resolved?.Resolve();
		}
	}
}
