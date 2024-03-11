using System.Linq;
using XNode;
using XNodeEditor;

namespace Narramancer {

	[CustomNodeEditor(typeof(IsNotNullNode))]
	public class IsNotNullNodeEditor : ResizableNodeEditor {

		public override bool HasCustomDroppedPortLogic() {
			var targetNode = target as IsNotNullNode;

			if (targetNode.DynamicInputs.Any() && targetNode.DynamicInputs.First().IsConnected) {
				return false;
			}
			return true;
		}

		public override void PerformCustomDroppedPortLogic(Node hoveredNode, NodePort draggedOutput) {

			var targetNode = hoveredNode as IsNotNullNode;

			if (targetNode.DynamicInputs.Any() && targetNode.DynamicInputs.First().IsConnected ) {
				return;
			}

			var nodePortType = draggedOutput.ValueType;
			if (AssemblyUtilities.IsListType(nodePortType)) {
				var innerType = AssemblyUtilities.GetListInnerType(nodePortType);
				if (innerType != null) {
					targetNode.ObjectType.Type = innerType;
					targetNode.ObjectType.List = true;
				}
			}
			else {
				targetNode.ObjectType.Type = nodePortType;
				targetNode.ObjectType.List = false;
			}
			var listPort = targetNode.DynamicInputs.First();
			listPort.Connect(draggedOutput);

		}
	}
}