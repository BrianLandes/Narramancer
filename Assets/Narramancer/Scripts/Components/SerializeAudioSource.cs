using UnityEngine;

namespace Narramancer {

	[RequireComponent(typeof(AudioSource))]
	public class SerializeAudioSource : SerializableMonoBehaviour {

		private AudioSource audioSource = default;

		public override void Awake() {
			base.Awake();
			audioSource = gameObject.GetComponent<AudioSource>();
		}

		public override void Serialize(StoryInstance story) {
			base.Serialize(story);

			story.SaveTable.Set(Key("playing"), audioSource.isPlaying);
			story.SaveTable.Set(Key("volume"), audioSource.volume);
			story.SaveTable.Set(Key("pitch"), audioSource.pitch);
			story.SaveTable.Set(Key("loop"), audioSource.loop);
			if (audioSource.isPlaying) {
				var clip = audioSource.clip;
				if (clip != null) { // this can happen if the AudioSource is only playing one shot audios
					story.SaveTable.Set(Key("clip"), clip);
				}

				story.SaveTable.Set(Key("time"), audioSource.time);
			}

		}

		public override void Deserialize(StoryInstance map) {
			base.Deserialize(map);
			audioSource = gameObject.GetComponent<AudioSource>();

			audioSource.volume = map.SaveTable.GetAndRemove<float>(Key("volume"));
			audioSource.pitch = map.SaveTable.GetAndRemove<float>(Key("pitch"));
			audioSource.loop = map.SaveTable.GetAndRemove<bool>(Key("loop"));
			var playing = map.SaveTable.GetAndRemove<bool>(Key("playing"));
			if (playing) {
				audioSource.time = map.SaveTable.GetAndRemove<float>(Key("time"));
				audioSource.clip = map.SaveTable.GetAndRemove<AudioClip>(Key("clip"));
				audioSource.Play();
			}
		}
	}
}