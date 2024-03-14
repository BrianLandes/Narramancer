using UnityEngine;
using UnityEngine.UI;

namespace Narramancer {

	[CreateNodeMenu("GameObject/Set Text")]
	public class SetTextNode : ChainedRunnableNode {

		[SerializeField]
		[Input(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.InheritedAny)]
		private Text text = default;

		[SerializeField]
		[Input(ShowBackingValue.Unconnected, ConnectionType.Override)]
		private string value = default;

		public override void Run(NodeRunner runner) {
			base.Run(runner);
			var text = GetInputValue(runner.Blackboard, nameof(this.text), this.text);
			if (text != null) {
				var value = GetInputValue(runner.Blackboard, nameof(this.value), this.value);
				text.text = value;
			}
		}
	}
}
