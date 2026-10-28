using System;
using UnityEngine;

namespace Narramancer {

	/// <summary>
	/// Position, rotation and scale. Saved <em>local</em> rather than world — the old saver wrote world values,
	/// which made the result depend on whether the parent had been restored yet.
	/// </summary>
	public class TransformSaver : ComponentSaver<Transform, TransformSaver.State> {

		[Serializable]
		public class State {
			public Vector3 localPosition;
			public Quaternion localRotation;
			public Vector3 localScale;
		}

		public override string Key => "transform";

		public override State Capture(Transform transform) {
			return new State {
				localPosition = transform.localPosition,
				localRotation = transform.localRotation,
				localScale = transform.localScale,
			};
		}

		public override void Apply(Transform transform, State state) {
			transform.localPosition = state.localPosition;
			transform.localRotation = state.localRotation;
			transform.localScale = state.localScale;
		}
	}
}
