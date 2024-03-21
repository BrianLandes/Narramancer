using UnityEngine;

namespace Narramancer {

	[RequireComponent(typeof(Animation))]
	public class SerializeAnimation : SerializableMonoBehaviour {

		[SerializeMonoBehaviourField]
		private Promise promise = default;

		[SerializeMonoBehaviourField]
		private bool playing = false;

		private Animation animationComponent = default;

		public override void Awake() {
			base.Awake();
			animationComponent = gameObject.GetComponent<Animation>();
		}

		private void AddClipMaybe(AnimationClip animationClip) {
			var animationState = animationComponent[animationClip.name];
			if (animationState == null) {
				animationComponent.AddClip(animationClip, animationClip.name);
			}
		}

		public Promise Play(AnimationClip animationClip) {

			AddClipMaybe(animationClip);

			animationComponent.Play(animationClip.name);


			playing = true;
			promise = new Promise();
			return promise;
		}

		private void Update() {
			if (playing) {
				if ( !animationComponent.isPlaying ) {
					playing = false;
					var promise = this.promise;
					this.promise = null;
					promise.Resolve();
				}
			}
		}

		public override void Serialize(StoryInstance story) {
			base.Serialize(story);

			if (playing) {

				var enumerator = animationComponent.GetEnumerator();
				while (enumerator.MoveNext()) {
					var animationState = enumerator.Current as AnimationState;
					if (animationState != null ) {
						story.SaveTable.Set(Key("clip"), animationState.clip);
						story.SaveTable.Set(Key("speed"), animationState.speed);
						story.SaveTable.Set(Key("time"), animationState.time);
						story.SaveTable.Set(Key("wrapMode"), animationState.wrapMode);
						break;
					}
				}
			}

		}

		public override void Deserialize(StoryInstance map) {
			base.Deserialize(map);
			animationComponent = gameObject.GetComponent<Animation>();

			if (playing) {
				var animationClip = map.SaveTable.GetAndRemove<AnimationClip>(Key("clip"));
				AddClipMaybe(animationClip);
				animationComponent.Play(animationClip.name);
				var animationState = animationComponent[animationClip.name];
				animationState.speed = map.SaveTable.GetAndRemove<float>(Key("speed"));
				animationState.time = map.SaveTable.GetAndRemove<float>(Key("time"));
				animationState.wrapMode = map.SaveTable.GetAndRemove<WrapMode>(Key("wrapMode"));
			}
		}
	}
}