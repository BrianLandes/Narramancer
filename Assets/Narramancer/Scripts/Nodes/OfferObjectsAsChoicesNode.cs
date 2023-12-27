
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {

	[NodeWidth(250)]
	[CreateNodeMenu("Flow/Elements As Choices")]
	public class OfferObjectsAsChoicesNode : RunnableNode {

		[SerializeField]
		private SerializableType type = new SerializableType();

		[Output(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Inherited)]
		[NodeTrianglePortHandle]
		[SerializeField]
		private RunnableNode runWhenObjectSelected = default;

		[Output(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Inherited)]
		[NodeTrianglePortHandle]
		[SerializeField]
		private RunnableNode runWhenBackSelected = default;

		[SerializeField]
		[RequireInputFromSerializableType(nameof(type), "element")]
		[RequireOutput(typeof(string), "display name")]
		private ValueVerb displayNamePredicate;

		[SerializeField]
		[RequireInputFromSerializableType(nameof(type), "element")]
		[RequireOutput(typeof(bool), "enabled")]
		private ValueVerb enabledPredicate;

		// TODO: Predicate for Custom Color
		// TODO: Predicate for Show if Disabled
		// TODO: Show a disabled choice if there are no elements that says 'None'
		// TODO: allow 'Back' text to be an input

		[SerializeField]
		private bool showIfDisabled = true;

		private const string INPUT_ELEMENTS = "Input Elements";
		private const string INPUT_LIST = "Input List";
		private const string SELECTED_ELEMENT = "Selected Element";

		private string ElementKey => Blackboard.UniqueKey(this, "Element");

		protected override void Init() {
			type.OnChanged -= UpdatePorts;
			type.OnChanged += UpdatePorts;
		}

		public override void UpdatePorts() {

			if (type.Type == null) {
				ClearDynamicPorts();
			}
			else {
				var keepPorts = new List<NodePort>();

				var inputPortElements = this.GetOrAddDynamicInput(type.Type, INPUT_ELEMENTS, ConnectionType.Multiple);
				keepPorts.Add(inputPortElements);

				var inputPort = this.GetOrAddDynamicInput(type.TypeAsList, INPUT_LIST);
				keepPorts.Add(inputPort);

				var outputPort = this.GetOrAddDynamicOutput(type.Type, SELECTED_ELEMENT);
				keepPorts.Add(outputPort);

				this.ClearDynamicPortsExcept(keepPorts);
			}

			base.UpdatePorts();
		}

		public override void Run(NodeRunner runner) {
			runner.Suspend();

			var choicePrinter = IChoicePrinter.GetChoicePrinter();
			choicePrinter.ClearChoices();

			var elementsList = new List<object>();

			var elementPort = GetInputPort(INPUT_ELEMENTS);
			if (elementPort.IsConnected) {
				var inputElements = elementPort.GetInputValues(runner.Blackboard);
				elementsList.AddRange(inputElements);
			}

			var listPort = GetInputPort(INPUT_LIST);
			if (listPort.IsConnected) {
				var inputList = listPort.GetInputValue(runner.Blackboard);
				if (inputList != null) {
					var inputListAsList = AssemblyUtilities.ToListOfObjects(inputList);
					elementsList.AddRange(inputListAsList);
				}
			}

			foreach (var element in elementsList) {

				var enabled = enabledPredicate == null || enabledPredicate.RunForValue<bool>(runner.Blackboard, type.Type, element);
				if (enabled) {

					var displayText = element.ToString();
					if (displayNamePredicate != null) {
						displayText = displayNamePredicate.RunForValue<string>(runner.Blackboard, type.Type, element);
					}
					choicePrinter.AddChoice(displayText, () => {
						runner.Blackboard.Set(ElementKey, element);
						var nextNode = GetRunnableNodeFromPort(nameof(runWhenObjectSelected));
						runner.Resume(nextNode);
					});
				}
				else
				if (showIfDisabled) {
					var displayText = element.ToString();
					if (displayNamePredicate != null) {
						displayText = displayNamePredicate.RunForValue<string>(runner.Blackboard, type.Type, element);
					}
					choicePrinter.AddDisabledChoice(displayText);
				}
			}

			choicePrinter.AddChoice("Back", () => {
				runner.Blackboard.Set(ElementKey, null, type.Type);
				var nextNode = GetRunnableNodeFromPort(nameof(runWhenBackSelected));
				runner.Resume(nextNode);
			});

			choicePrinter.ShowChoices();
		}

		public override object GetValue(object context, NodePort port) {
			if (Application.isPlaying && port.fieldName.Equals(SELECTED_ELEMENT)) {
				var blackboard = context as Blackboard;
				var element = blackboard.Get(ElementKey, type.Type);
				return element;
			}
			return base.GetValue(context, port);
		}
	}
}