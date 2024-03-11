
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	[NodeWidth(500)]
	[CreateNodeMenu("Flow/Choice (Option)")]
	public class ChoiceNode : Node {

		[SerializeField]
		[Input(connectionType = ConnectionType.Multiple,
			backingValue = ShowBackingValue.Never,
			typeConstraint = TypeConstraint.Inherited)]
		private ChoiceNode thisChoice = default;
		public static string ThisChoiceField => nameof(thisChoice);

		[Output(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		[SameLine]
		[SerializeField]
		private RunnableNode thenRunNode = default;

		[Input(connectionType = ConnectionType.Override,
			backingValue = ShowBackingValue.Unconnected,
			typeConstraint = TypeConstraint.Inherited)]
		public bool enabled = true;


		[SerializeField]
		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		private string displayText = "";

		[SerializeField]
		private ToggleableValue<Color> customColor = new ToggleableValue<Color>(false, Color.white);

		[SerializeField]
		private bool displayWhenDisabled = false;
		public bool DisplayWhenDisabled => displayWhenDisabled;

		/// <summary>
		/// Returns the node itself that is connected to the 'thenRunNode' port (if there is one, null otherwise).
		/// </summary>
		public RunnableNode GetNextNode() {
			var port = GetOutputPort(nameof(thenRunNode));
			if (port.ValueType.IsAssignableFrom(typeof(RunnableNode))) {
				var connections = port.GetConnections();
				if (connections.Count == 0) {
					return null;
				}
				// the 'value' is the node itself
				var node = connections[0].node as RunnableNode;
				return node;
			}
			return null;
		}

		public override object GetValue(INodeContext context, NodePort port) {

			if (port.fieldName == nameof(thisChoice)) {
				// the 'value' is the node itself
				return this;
			}

			if (port.ValueType.IsAssignableFrom(typeof(RunnableNode))) {
				var connections = port.GetConnections();
				if (connections.Count == 0) {
					return null;
				}
				// the 'value' is the node itself
				return connections[0].node;
			}

			return null;
		}


		public bool IsConditionMet(INodeContext context) {
			return GetInputValue(context, nameof(enabled), enabled);
		}

		public string GetDisplayText(INodeContext context, bool applyColor = true) {
			var displayText = GetInputValue(context, nameof(this.displayText), this.displayText);
			if (applyColor && customColor.activated) {
				displayText = $"<color=#{ColorUtility.ToHtmlStringRGB(customColor.value)}>{displayText}</color>";
			}
			return displayText;
		}

	}
}