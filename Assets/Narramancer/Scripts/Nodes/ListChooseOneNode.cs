
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using XNode;

namespace Narramancer {

	public class ListChooseOneNode : ChainedRunnableNode {

		[SerializeField]
		private SerializableType listType = new SerializableType();

		private const string LIST = "List";
		private const string CHOSEN_ELEMENT = "Chosen Element";

		private MethodInfo toArrayMethod;

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

				var outputPort = this.GetOrAddDynamicOutput(listType.Type, CHOSEN_ELEMENT);
				keepPorts.Add(outputPort);

				this.ClearDynamicPortsExcept(keepPorts);

			}

			base.UpdatePorts();
		}

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			var inputPort = GetInputPort(LIST);
			var inputValue = inputPort.GetInputValue(runner.Blackboard);

			Type type = inputValue.GetType();

			toArrayMethod = type.GetMethod("ToArray");

			var inputArrayObjects = toArrayMethod.Invoke(inputValue, null);

			var inputArray = inputArrayObjects as object[];

			var chosenElementKey = Blackboard.UniqueKey(this, CHOSEN_ELEMENT);

			if (inputArray.Length >= 1) {
				var chosenElement = inputArray.ChooseOne();

				// TODO: account for types that are NOT serializable
				runner.Blackboard.Set(chosenElementKey, chosenElement);
			}
		}

		public override object GetValue(object context, NodePort port) {
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