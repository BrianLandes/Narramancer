using System;
using UnityEngine;

namespace Narramancer {

	/// <summary>The clip a legacy <see cref="Animation"/> is playing, and where in it. Waiting on it is <see cref="AnimationPlayer"/>'s job.</summary>
	public class AnimationSaver : ComponentSaver<Animation, AnimationSaver.State> {

		[Serializable]
		public class State {
			public AnimationClip clip;
			public float time;
			public float speed;
			public WrapMode wrapMode;
		}

		public override string Key => "animation";

		public override State Capture(Animation animation) {
			var state = new State();
			foreach (AnimationState animationState in animation) {
				if (animation.IsPlaying(animationState.name)) {
					if (Serialization.PersistentId.Of(animationState.clip) == null) {
						Debug.LogWarning($"The clip '{animationState.clip.name}' on '{animation.name}' cannot be saved: it is not in a Resources folder or the asset manifest. The animation will not resume.", animation);
						break;
					}
					state.clip = animationState.clip;
					state.time = animationState.time;
					state.speed = animationState.speed;
					state.wrapMode = animationState.wrapMode;
					break;
				}
			}
			return state;
		}

		public override void Apply(Animation animation, State state) {
			if (state.clip == null) {
				return;
			}
			if (animation[state.clip.name] == null) {
				animation.AddClip(state.clip, state.clip.name);
			}
			animation.Play(state.clip.name);
			var animationState = animation[state.clip.name];
			animationState.time = state.time;
			animationState.speed = state.speed;
			animationState.wrapMode = state.wrapMode;
		}
	}
}
