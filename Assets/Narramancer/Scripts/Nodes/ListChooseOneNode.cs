using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("List/Choose One Random Element from List")]
	public class ListChooseOneNode : ChainedRunnableNode, IListTypeNode {

		[SerializeField]
		private SerializableType listType = new SerializableType();
		public SerializableType ListType => listType;

		private const string LIST = "List";
		private const string CHOSEN_ELEMENT = "Chosen Element";

		protected override void Init() {
			listType.OnChanged -= UpdatePorts;
			listType.OnChanged += UpdatePorts;
		}

		public override void UpdatePorts() {

			if (listType.Type == null) {
				ClearDynamicPorts();
			}
			else {

				var keepPorts = new List<NodePort>();

				var inputPort = this.GetOrAddDynamicInput(listType.TypeAsList, LIST);
				keepPorts.Add(inputPort);

				var outputPort = this.GetOrAddDynamicOutput(listType.Type, CHOSEN_ELEMENT, sameLine:true);
				keepPorts.Add(outputPort);

				this.ClearDynamicPortsExcept(keepPorts);

			}

			base.UpdatePorts();
		}

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			var inputPort = GetInputPort(LIST);
			var inputValue = inputPort.GetInputValueObjectList(runner.Blackboard);

			var chosenElementKey = Blackboard.UniqueKey(this, CHOSEN_ELEMENT);

			if (inputValue.Count >= 1) {
				var chosenElement = inputValue.ChooseOne();

				// TODO: account for types that are NOT serializable
				runner.Blackboard.Set(chosenElementKey, chosenElement);
			}
		}

		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying && port.fieldName.Equals(CHOSEN_ELEMENT)) {

				var blackboard = context as Blackboard;

				var chosenElementKey = Blackboard.UniqueKey(this, CHOSEN_ELEMENT);
				var chosenElement = blackboard.Get(chosenElementKey, listType.Type);

				return chosenElement;
			}
			return base.GetValue(context, port);
		}
	}
}