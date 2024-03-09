using UnityEngine;

namespace Narramancer {

	[CreateNodeMenu("GameObject/Set Image Sprite")]
	public class SetGameObjectActiveNode : ChainedRunnableNode {

		[SerializeField]
		[Input(ShowBackingValue.Never, ConnectionType.Override)]
		private GameObject gameObject = default;

		[SerializeField]
		[Input(ShowBackingValue.Unconnected, ConnectionType.Override)]
		private bool active = true;

		public override void Run(NodeRunner runner) {
			base.Run(runner);
			var gameObject = GetInputValue(runner.Blackboard, nameof(this.gameObject), this.gameObject);

			if (gameObject != null) {
				var active = GetInputValue(runner.Blackboard, nameof(this.active), this.active);

				gameObject.SetActive(active);
			}
		}
	}
}
