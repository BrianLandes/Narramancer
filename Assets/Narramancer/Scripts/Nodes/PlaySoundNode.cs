using UnityEngine;
using XNode;

namespace Narramancer {
	public class PlaySoundNode : ChainedRunnableNode {

		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField]
		private AudioClip audioClip = default;

		[Input(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField]
		private AudioSource audioSource = default;

		public enum PlayType {
			Music,
			OneShot
		}
		[SerializeField]
		[NodeEnum]
		private PlayType playType = PlayType.Music;

		[SerializeField]
		private bool loop = true;

		[SerializeField]
		private bool wait = false;

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			var audioClip = GetInputValue(runner.Blackboard, nameof(this.audioClip), this.audioClip);
			if (audioClip == null) {
				Debug.Log("AudioClip was null", this);
				return;
			}

			var audioSource = GetInputValue(runner.Blackboard, nameof(this.audioSource), this.audioSource);

			if (audioSource == null) {
				audioSource = FindObjectOfType<AudioSource>();
			}
			if (audioSource == null) {
				var newAudioSource = new GameObject("Audio Source");
				audioSource = newAudioSource.AddComponent<AudioSource>();
			}

			switch (playType) {
				case PlayType.Music:
					audioSource.Stop();
					audioSource.clip = audioClip;
					audioSource.loop = loop;
					audioSource.Play();
					break;
				case PlayType.OneShot:
					audioSource.PlayOneShot(audioClip);
					break;
			}

			if (wait) {
				runner.Suspend();
				var time = audioClip.length;
				NarramancerSingleton.Instance.MakeTimer(time).WhenDone(() => {
					runner.Resume();
				});
			}
		}
	}
}