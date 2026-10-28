using System;
using UnityEngine;

namespace Narramancer {

	/// <summary>Volume, pitch, looping, and — if it was playing — the clip and how far into it.</summary>
	public class AudioSourceSaver : ComponentSaver<AudioSource, AudioSourceSaver.State> {

		[Serializable]
		public class State {
			public bool enabled;
			public float volume;
			public float pitch;
			public bool loop;
			public bool playing;
			public AudioClip clip;
			public float time;
		}

		public override string Key => "audioSource";

		public override State Capture(AudioSource audioSource) {
			var state = new State {
				enabled = audioSource.enabled,
				volume = audioSource.volume,
				pitch = audioSource.pitch,
				loop = audioSource.loop,
				playing = audioSource.isPlaying,
			};
			// A source playing only one-shots has no clip of its own; there is nothing to resume.
			if (state.playing && audioSource.clip != null) {
				if (Serialization.PersistentId.Of(audioSource.clip) == null) {
					// Writing the reference anyway would fail the whole save, not just this clip.
					Debug.LogWarning($"The clip '{audioSource.clip.name}' on '{audioSource.name}' cannot be saved: it is not in a Resources folder or the asset manifest. It will not resume.", audioSource);
					state.playing = false;
				}
				else {
					state.clip = audioSource.clip;
					state.time = audioSource.time;
				}
			}
			return state;
		}

		public override void Apply(AudioSource audioSource, State state) {
			audioSource.enabled = state.enabled;
			audioSource.volume = state.volume;
			audioSource.pitch = state.pitch;
			audioSource.loop = state.loop;
			if (state.playing && state.clip != null) {
				audioSource.clip = state.clip;
				audioSource.time = state.time;
				audioSource.Play();
			}
			else {
				audioSource.Stop();
			}
		}
	}
}
