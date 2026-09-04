
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {

	[NodeWidth(300)]
	[CreateNodeMenu("Flow/Elements As Choices")]
	public class OfferObjectsAsChoicesNode : RunnableNode {

		[SerializeField]
		private SerializableType type = new SerializableType();
		public static string TypeFieldName => nameof(type);
		public SerializableType ElementType => type;

		[Output(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField]
		private RunnableNode runWhenObjectSelected = default;
		public static string RunWhenObjectSelectedFieldName => nameof(runWhenObjectSelected);


		[SerializeField]
		[Tooltip("Whether to append an option to the list of choices for going 'Back'")]
		private bool addOptionForBack = false;
		public static string AddOptionForBackFieldName => nameof(addOptionForBack);


		[Output(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField]
		private RunnableNode runWhenBackSelected = default;
		public static string RunWhenBackSelectedFieldName => nameof(runWhenBackSelected);

		[SerializeField]
		private bool useValueVerbForDisplayname = false;
		public static string UseValueVerbForDisplaynameFieldName => nameof(useValueVerbForDisplayname);

		[SerializeField]
		[RequireInputFromSerializableType(nameof(type), "element")]
		[RequireOutput(typeof(string), "display name")]
		private ValueVerb displayNamePredicate = default;
		public static string DisplayNamePredicateFieldName => nameof(displayNamePredicate);

		[SerializeField]
		private bool useValueVerbForEnabled = false;
		public static string UseValueVerbForEnabledFieldName => nameof(useValueVerbForEnabled);

		[SerializeField]
		[RequireInputFromSerializableType(nameof(type), "element")]
		[RequireOutput(typeof(bool), "enabled")]
		private ValueVerb enabledPredicate = default;
		public static string EnabledPredicateFieldName => nameof(enabledPredicate);

		[SerializeField]
		private bool useToolTip = false;
		public static string UseToolTipFieldName => nameof(useToolTip);

		[SerializeField]
		[RequireInputFromSerializableType(nameof(type), "element")]
		[RequireOutput(typeof(string), "toolTip")]
		private ValueVerb toolTipPredicate = default;
		public static string ToolTipPredicateFieldName => nameof(toolTipPredicate);

		// TODO: Predicate for Custom Color
		// TODO: Predicate for Show if Disabled
		// TODO: Show a disabled choice if there are no elements that says 'None'
		// TODO: allow 'Back' text to be an input

		[SerializeField]
		[Tooltip("Whether to show the option even if it is disabled")]
		private bool showIfDisabled = true;
		public static string ShowIfDisabledFieldName => nameof(showIfDisabled);

		public const string INPUT_ELEMENTS = "Input Elements";
		public const string INPUT_LIST = "Input List";
		public const string SELECTED_ELEMENT = "Selected Element";

		private string ElementKey => Blackboard.UniqueKey(this, "Element");

		/// <summary>Ids for this node's continuations. Written into saves verbatim.</summary>
		private const string ChoiceCallbackPrefix = "choice:";

		private const string BackCallbackId = "back";

		/// <summary>Where the element behind offered choice <paramref name="index"/> is parked.</summary>
		private string OptionKey(int index) => Blackboard.UniqueKey(this, $"Option {index}");

		public override void OnContinuation(string callbackId, NodeRunner runner) {
			if (runner == null) {
				return;
			}

			if (callbackId == BackCallbackId) {
				runner.Blackboard.Set(ElementKey, null, type.Type);
				runner.Resume(GetRunnableNodeFromPort(nameof(runWhenBackSelected)));
				return;
			}

			if (!callbackId.StartsWith(ChoiceCallbackPrefix, System.StringComparison.Ordinal)
				|| !int.TryParse(callbackId.Substring(ChoiceCallbackPrefix.Length), out var index)) {
				return;
			}

			// GetAndRemove so the options this node parked do not linger in the save after a choice is made.
			runner.Blackboard.Set(ElementKey, runner.Blackboard.GetAndRemove(OptionKey(index), type.Type), type.Type);
			runner.Resume(GetRunnableNodeFromPort(nameof(runWhenObjectSelected)));
		}

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

				if (useValueVerbForEnabled && enabledPredicate != null) {
					var baseInputPort = enabledPredicate.GetInput(type.Type);
					var baseOutputPort = enabledPredicate.GetOutput<bool>();

					var miscPorts = this.GetOrAddPortsForGraph(enabledPredicate, new[] { baseInputPort, baseOutputPort });
					keepPorts.AddRange(miscPorts);
				}

				this.ClearDynamicPortsExcept(keepPorts);
			}

			base.UpdatePorts();
		}

		public override void Run(NodeRunner runner) {
			runner.Suspend();

			var choicePrinter = ChoicePrinter.GetChoicePrinter();
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

			var useEnabledPredicate = useValueVerbForEnabled && enabledPredicate;

			// Each offered element is written to the blackboard under its own key, and the choice's
			// continuation carries only the index. The element itself is genuine captured state — the one
			// thing in the node library a closure was really holding — and this is how it becomes data.
			var optionIndex = 0;

			foreach (var element in elementsList) {
				if (useEnabledPredicate && enabledPredicate != null) {
					foreach (var inputPort in DynamicInputs) {
						var verbPort = enabledPredicate.GetInput(inputPort.ValueType, inputPort.fieldName);
						verbPort?.AssignValueFromNodePort(runner.Blackboard, inputPort);
					}
				}
				var enabled = !useEnabledPredicate || enabledPredicate.RunForValue<bool>(runner.Blackboard, type.Type, element);
				if (enabled) {

					var displayText = element.ToString();
					if (useValueVerbForDisplayname && displayNamePredicate != null) {
						displayText = displayNamePredicate.RunForValue<string>(runner.Blackboard, type.Type, element);
					}
					var toolTip = string.Empty;
					if (useToolTip && toolTipPredicate != null) {
						toolTip = toolTipPredicate.RunForValue<string>(runner.Blackboard, type.Type, element);
					}
					runner.Blackboard.Set(OptionKey(optionIndex), element, type.Type);
					choicePrinter.AddChoice(displayText, toolTip,
						Continuation.NodeCallbackTo(this, ChoiceCallbackPrefix + optionIndex, runner));
					optionIndex++;
				}
				else
				if (showIfDisabled) {
					var displayText = element.ToString();
					if (useValueVerbForDisplayname && displayNamePredicate != null) {
						displayText = displayNamePredicate.RunForValue<string>(runner.Blackboard, type.Type, element);
					}
					choicePrinter.AddDisabledChoice(displayText);
				}
			}

			if (addOptionForBack) {
				choicePrinter.AddChoice("Back", string.Empty,
					Continuation.NodeCallbackTo(this, BackCallbackId, runner));
			}


			choicePrinter.ShowChoices();
		}

		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying && port.fieldName.Equals(SELECTED_ELEMENT)) {
				var blackboard = context as Blackboard;
				var element = blackboard.Get(ElementKey, type.Type);
				return element;
			}
			return base.GetValue(context, port);
		}
	}
}