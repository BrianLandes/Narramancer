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

			var serializeAnimation = animationComponent.gameObject.GetComponent<SerializeAnimation>();
			if (serializeAnimation != null) {
				var promise = serializeAnimation.Play(animationClip);
				if (wait) {
					runner.Suspend();
					promise.WhenDone(() => {
						runner.Resume();
					});
				}
			}
			else {
				animationComponent.Play();
			}
			

			
		}
	}
}