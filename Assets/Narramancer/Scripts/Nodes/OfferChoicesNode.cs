
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {

	[NodeWidth(250)]
	[CreateNodeMenu("Flow/Choices (Offer)")]
	public class OfferChoicesNode : RunnableNode {

		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
		[SameLine]
		public ChoiceNode choiceNodes;


		public IEnumerable<ChoiceNode> ChoiceNodes {
			get {
				var port = GetOutputPort(nameof(choiceNodes));
				var connections = port.GetConnections();
				if (connections == null) {
					return Enumerable.Empty<ChoiceNode>();
				}
				var connectedNodes = connections.Select(connection => connection.node).ToList();
				var connectedCoiceNodes = connectedNodes.Cast<ChoiceNode>().ToList();
				return connectedCoiceNodes.OrderBy(choiceNode => choiceNode.position.y);
			}
		}


		public override void Run(NodeRunner runner) {
			runner.Suspend();

			var choicePrinter = ChoicePrinter.GetChoicePrinter();
			choicePrinter.ClearChoices();

			foreach (var choiceNode in ChoiceNodes) {
				if (choiceNode.IsConditionMet(runner.Blackboard)) {
					var displayText = choiceNode.GetDisplayText(runner.Blackboard);
					choicePrinter.AddChoice(displayText, () => {

						var nextNode = choiceNode.GetNextNode();
						runner.Resume(nextNode);
					});
				}
				else
				if (choiceNode.DisplayWhenDisabled) {
					var displayText = choiceNode.GetDisplayText(runner.Blackboard);
					choicePrinter.AddDisabledChoice(displayText);
				}
			}

			choicePrinter.ShowChoices();
		}

		public Node AddChoiceNode() {
			var position = this.position + new Vector2(300, 0);
			var lastNode = ChoiceNodes.LastOrDefault();
			if (lastNode != null) {
				position = lastNode.position + new Vector2(0, 200);
			}

			var newNode = graph.AddNode<ChoiceNode>(position);
			var choicePort = newNode.GetInputPort(ChoiceNode.ThisChoiceField);
			var thisPort = GetOutputPort(nameof(choiceNodes));
			thisPort.Connect(choicePort);
			return newNode;
		}

	}
}