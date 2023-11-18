using UnityEngine;
using UnityEngine.UI;

namespace Narramancer {

	[CreateNodeMenu("GameObject/Set Image Sprite")]
	public class SetImageSpriteNode : ChainedRunnableNode {

		[SerializeField]
		[Input(ShowBackingValue.Never, ConnectionType.Override)]
		private Image image = default;

		[SerializeField]
		[Input(ShowBackingValue.Unconnected, ConnectionType.Override)]
		private Sprite sprite = default;

		public override void Run(NodeRunner runner) {
			base.Run(runner);
			var image = GetInputValue(runner.Blackboard, nameof(this.image), this.image);
			if (image != null) {
				var sprite = GetInputValue(runner.Blackboard, nameof(this.sprite), this.sprite);
				image.sprite = sprite;
			}
		}
	}
}
