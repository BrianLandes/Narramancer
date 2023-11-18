
using System.Linq;
using XNodeEditor;

namespace Narramancer {

	[CustomNodeEditor(typeof(OutputNode))]
	public class OutputNodeEditor : NodeEditor {

		public override void OnBodyGUI() {
			var outputNode = target as OutputNode;
			var ioGraph = outputNode.graph as VerbGraph;
			bool NodeAsGraphPort(NarramancerPort port) {
				var nodePort = outputNode.GetInputPort(port.Name);
				if (nodePort == null) {
					return false;
				}
				if (nodePort.ValueType != port.Type) {
					return false;
				}
				return true;
			}
			var needsUpdate = ioGraph.Outputs.Count != outputNode.Inputs.Count() || ioGraph.Outputs.Any(graphOutput => !NodeAsGraphPort(graphOutput));
			if (needsUpdate) {
				outputNode.RebuildInputPorts();
			}

			base.OnBodyGUI();
		}
	}
}