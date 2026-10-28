using UnityEngine;

namespace Narramancer {

	/// <summary>
	/// Plays a clip on a legacy <see cref="Animation"/> and resolves a promise when it ends, surviving a save
	/// part-way through. Added on demand by <see cref="PlayAnimationNode"/>. The clip and its playhead are the
	/// <see cref="AnimationSaver"/> driver's; this keeps only the waiting.
	/// </summary>
	[RequireComponent(typeof(Animation))]
	public class AnimationPlayer : MonoBehaviour {

		[Save]
		private Promise promise = default;

		[Save]
		private bool playing = false;

		/// <summary>The object's AnimationPlayer, adding one if it has none.</summary>
		public static AnimationPlayer On(Animation animation) {
			if (!animation.TryGetComponent<AnimationPlayer>(out var player)) {
				player = animation.gameObject.AddComponent<AnimationPlayer>();
				if (!animation.TryGetComponent<Saveable>(out _)) {
					Debug.LogWarning($"'{animation.name}' has no {nameof(Saveable)}, so a save made while it is animating will not resume the animation.", animation);
				}
			}
			return player;
		}

		public Promise Play(AnimationClip clip) {
			var animation = GetComponent<Animation>();
			if (animation[clip.name] == null) {
				animation.AddClip(clip, clip.name);
			}
			animation.Play(clip.name);

			promise?.Resolve();
			playing = true;
			promise = new Promise();
			return promise;
		}

		private void Update() {
			if (playing && !GetComponent<Animation>().isPlaying) {
				playing = false;
				var resolved = promise;
				promise = null;
				resolved?.Resolve();
			}
		}
	}
}
