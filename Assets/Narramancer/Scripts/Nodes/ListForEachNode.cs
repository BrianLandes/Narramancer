
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("List/For Each Element in List")]
	public class ListForEachNode : ChainedRunnableNode, IListTypeNode {

		[SerializeField]
		private SerializableType listType = new SerializableType();
		public SerializableType ListType => listType;

		private const string INPUT_ELEMENTS = "Elements";
		private const string INPUT_LIST = "List";
		private const string CURRENT_ELEMENT = "Element";
		private const string CURRENT_INDEX = "Index";

		private string IndexKey => Blackboard.UniqueKey(this, "index");

		private string ElementKey => Blackboard.UniqueKey(this, "currentElement");
		private string ListKey => Blackboard.UniqueKey(this, "list");

		protected override void Init() {
			listType.OnChanged -= RebuildPorts;
			listType.OnChanged += RebuildPorts;
		}

		private void RebuildPorts() {

			if (listType.Type == null) {
				ClearDynamicPorts();
				return;
			}

			List<NodePort> existingPorts = new List<NodePort>();

			var elementType = listType.Type;

			var inputListPort = this.GetOrAddDynamicInput(listType.TypeAsList, INPUT_LIST);
			existingPorts.Add(inputListPort);

			var inputPort = this.GetOrAddDynamicInput(elementType, INPUT_ELEMENTS, ConnectionType.Multiple);
			existingPorts.Add(inputPort);

			var outputListPort = this.GetOrAddDynamicOutput(elementType, CURRENT_ELEMENT);
			existingPorts.Add(outputListPort);

			var indexPort = this.GetOrAddDynamicOutput(typeof(int), CURRENT_INDEX);
			existingPorts.Add(indexPort);

			this.ClearDynamicPortsExcept(existingPorts);

		}

		private List<object> BuildList(INodeContext blackboard) {

			var inputList = new List<object>();

			var inputListPort = GetInputPort(INPUT_LIST);
			var value = inputListPort.GetInputValue(blackboard);
			if (value != null) {
				inputList.AddRange(AssemblyUtilities.ToListOfObjects(value));
			}


			var inputElementPort = GetInputPort(INPUT_ELEMENTS);
			var inputElements = inputElementPort.GetInputValues(blackboard);

			if (inputElements != null) {
				inputList.AddRange(inputElements);
			}
			return inputList;
		}

		public override void Run(NodeRunner runner) {
			var blackboard = runner.Blackboard;

			var index = blackboard.GetInt(IndexKey);

			var list = blackboard.Get<List<object>>(ListKey);

			if (index == 0 || list == null) {
				list = BuildList(blackboard);
				blackboard.Set(ListKey, list);
			}


			if (index >= list.Count()) {
				blackboard.SetInt(IndexKey, 0);
				blackboard.Remove<List<object>>(ListKey);
				// allow the runner to resume / drop out
			}
			else {
				var currentElement = list.ElementAt(index);
				blackboard.Set(ElementKey, currentElement);
				blackboard.IncrementInt(IndexKey);

				// 'Push' this same node -> trigger the next iteration
				runner.Prepend(this);

				// 'Push' the next node when processing an element
				if (TryGetNextNode(out var nextNode)) {
					runner.Prepend(nextNode);
				}
			}

		}

		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying) {
				if (port.fieldName.Equals(CURRENT_ELEMENT)) {
					var blackboard = context as Blackboard;
					return blackboard.Get(ElementKey, listType.Type);
				}
				if (port.fieldName.Equals(CURRENT_INDEX)) {
					var blackboard = context as Blackboard;
					return blackboard.GetInt(IndexKey) - 1;
				}
			}
			return base.GetValue(context, port);
		}

	}
}