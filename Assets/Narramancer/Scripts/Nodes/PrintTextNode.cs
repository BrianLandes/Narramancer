using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Narramancer {
	public class PrintTextNode : ChainedRunnableNode {

		[Input(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField]
		private TextPrinter textPrinter = default;
		public static string TextPrinterFieldName => nameof(textPrinter);

		[SerializeField, HideInInspector]
		private int width = 300;
		public static string WidthFieldName => nameof(width);

		[SerializeField, HideInInspector]
		private int height = 80;
		public static string HeightFieldName => nameof(height);

		[SerializeField]
		private bool clearPreviousText = true;
		public static string ClearPreviousTextFieldName => nameof(clearPreviousText);

		[SerializeField]
		private bool waitForContinue = true;
		public static string WaitForContinueFieldName => nameof(waitForContinue);

		[SerializeField, HideInInspector]
		private bool enableRichText = false;
		public static string EnableRichTextFieldName => nameof(enableRichText);

		[TextArea(6, 12)]
		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		public string text = default;

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			if (waitForContinue) {
				runner.Suspend();
			}



			var inputText = GetInputValue(runner.Blackboard, nameof(text), text);

			foreach (var input in DynamicInputs) {
				var inputObject = input.GetInputValue(runner.Blackboard);
				var replacementText = string.Empty;
				if (inputObject != null) {
					replacementText = inputObject.ToString();
				}
				var expression = "{" + input.fieldName + "}";
				while (inputText.IndexOf(expression) >= 0) {
					inputText = inputText.Replace(expression, replacementText);
				}

			}

			#region Add text to Log
			NarramancerSingleton.Instance.StoryInstance.AddTextLog(inputText);
			#endregion

			var textPrinter = GetInputValue(runner.Blackboard, nameof(this.textPrinter), this.textPrinter);
			if (textPrinter == null) {
				textPrinter = GameObjectExtensions.FindObjectsOfType<TextPrinter>(true).FirstOrDefault(x => x.isMainTextPrinter);
			}
			textPrinter.SetText(inputText, () => {
				if (waitForContinue) {
					runner.Resume();
				}

			},
			clearPreviousText, waitForContinue);
		}
	}
}