using UnityEngine;

namespace Narramancer {
	public class PlayAnimationNode : ChainedRunnableNode {

		[Input(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField]
		private Animation animationComponent = default;

		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField]
		private AnimationClip animationClip = default;

		[SerializeField]
		private bool wait = true;

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			var animationComponent = GetInputValue(runner.Blackboard, nameof(this.animationComponent), this.animationComponent);
			var animationClip = GetInputValue(runner.Blackboard, nameof(this.animationClip), this.animationClip);

			if (animationClip != null) {
				var promise = AnimationPlayer.On(animationComponent).Play(animationClip);
				if (wait) {
					runner.Suspend();
					promise.WhenDone(Continuation.Resume(runner));
				}
			}
			else {
				animationComponent.Play();
			}
			

			
		}
	}
}