
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

			foreach (var element in elementsList) {
				
				var enabled = !useEnabledPredicate || enabledPredicate.RunForValue<bool>(runner.Blackboard, type.Type, element);
				if (enabled) {

					var displayText = element.ToString();
					if (useValueVerbForDisplayname && displayNamePredicate != null) {
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
					if (useValueVerbForDisplayname && displayNamePredicate != null) {
						displayText = displayNamePredicate.RunForValue<string>(runner.Blackboard, type.Type, element);
					}
					choicePrinter.AddDisabledChoice(displayText);
				}
			}

			if (addOptionForBack) {
				choicePrinter.AddChoice("Back", () => {
					runner.Blackboard.Set(ElementKey, null, type.Type);
					var nextNode = GetRunnableNodeFromPort(nameof(runWhenBackSelected));
					runner.Resume(nextNode);
				});
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