using UnityEngine;

namespace Narramancer {
	public class PlayAudioNode : ChainedRunnableNode {

		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField]
		private AudioClip audioClip = default;

		[Input(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField]
		private AudioSource audioSource = default;

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			var audioClip = GetInputValue(runner.Blackboard, nameof(this.audioClip), this.audioClip);

			var audioSource = GetInputValue(runner.Blackboard, nameof(this.audioSource), this.audioSource);

			if (audioSource == null) {
				audioSource = FindObjectOfType<AudioSource>();
			}
			if (audioSource == null) {
				var newAudioSource = new GameObject("Audio Source");
				audioSource = newAudioSource.AddComponent<AudioSource>();
			}
			if (audioSource.isPlaying) {
				audioSource.Stop();
			}
			audioSource.clip = audioClip;
			audioSource.Play();
		}
	}
}