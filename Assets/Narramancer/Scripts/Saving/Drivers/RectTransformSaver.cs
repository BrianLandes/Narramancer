using System;
using UnityEngine;

namespace Narramancer {

	/// <summary>A UI element's placement. Takes precedence over <see cref="TransformSaver"/>, since RectTransform derives from Transform.</summary>
	public class RectTransformSaver : ComponentSaver<RectTransform, RectTransformSaver.State> {

		[Serializable]
		public class State {
			public Vector3 anchoredPosition3D;
			public Vector2 sizeDelta;
			public Quaternion localRotation;
			public Vector3 localScale;
		}

		public override string Key => "rectTransform";

		public override State Capture(RectTransform rectTransform) {
			return new State {
				anchoredPosition3D = rectTransform.anchoredPosition3D,
				sizeDelta = rectTransform.sizeDelta,
				localRotation = rectTransform.localRotation,
				localScale = rectTransform.localScale,
			};
		}

		public override void Apply(RectTransform rectTransform, State state) {
			rectTransform.anchoredPosition3D = state.anchoredPosition3D;
			rectTransform.sizeDelta = state.sizeDelta;
			rectTransform.localRotation = state.localRotation;
			rectTransform.localScale = state.localScale;
		}
	}
}
