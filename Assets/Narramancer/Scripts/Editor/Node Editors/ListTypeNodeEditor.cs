using System.Linq;
using XNode;
using XNodeEditor;

namespace Narramancer {

	[CustomNodeEditor(typeof(ListAnyNode))]
	[CustomNodeEditor(typeof(ListAverageNode))]
	[CustomNodeEditor(typeof(ListCountNode))]
	[CustomNodeEditor(typeof(ListFilterNode))]
	[CustomNodeEditor(typeof(ListFirstNode))]
	[CustomNodeEditor(typeof(ListOrderNode))]
	[CustomNodeEditor(typeof(ListSubtractNode))]
	[CustomNodeEditor(typeof(ListSumNode))]
	[CustomNodeEditor(typeof(ListUnionNode))]
	[CustomNodeEditor(typeof(ListContainsNode))]
	public class ListTypeNodeEditor : NodeEditor {

		public override bool HasCustomDroppedPortLogic() {
			var targetNode = target as IListTypeNode;

			if (targetNode.DynamicInputs.Any() && targetNode.DynamicInputs.First().IsConnected) {
				return false;
			}
			return true;
		}

		public override void PerformCustomDroppedPortLogic(Node hoveredNode, NodePort draggedOutput) {

			var targetNode = hoveredNode as IListTypeNode;

			if (targetNode.DynamicInputs.Any() && targetNode.DynamicInputs.First().IsConnected ) {
				return;
			}

			var nodePortType = draggedOutput.ValueType;
			var innerType = AssemblyUtilities.GetListInnerType(nodePortType);
			if (innerType != null) {
				targetNode.ListType.Type = innerType;

				var listPort = targetNode.DynamicInputs.First();
				listPort.Connect(draggedOutput);
			}
			
		}
	}
}