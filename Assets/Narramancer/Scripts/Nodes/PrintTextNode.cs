using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Narramancer {
	public class PrintTextNode : ChainedRunnableNode {

		[TextArea(6, 12)]
		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		public string text;

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			runner.Suspend();

			var inputText = GetInputValue(runner.Blackboard, nameof(text), text);

			var textPrinter = this.FindObjectsOfType<TextPrinter>(true).FirstOrDefault();
			// TODO: cache textPrinter
			textPrinter.SetText(inputText, () => {
				runner.Resume();
			});
		}
	}
}