using System.Linq;
using XNode;

namespace Narramancer {

	[CustomNodeEditor(typeof(ListChooseOneNode))]
	[CustomNodeEditor(typeof(ListForEachNode))]
	public class ListTypeChainedRunnableNodeEditor : ChainedRunnableNodeEditor {

		public override bool HasCustomDroppedPortLogic() {
			var targetNode = target as IListTypeNode;

			if (targetNode.DynamicInputs.Any() && targetNode.DynamicInputs.First().IsConnected) {
				return false;
			}
			return true;
		}

		public override void PerformCustomDroppedPortLogic(Node hoveredNode, NodePort draggedOutput) {

			var targetNode = hoveredNode as IListTypeNode;

			if (targetNode.DynamicInputs.Any() && targetNode.DynamicInputs.First().IsConnected) {
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