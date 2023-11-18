
using XNodeEditor;

namespace Narramancer {
	[CustomNodeEditor(typeof(RunValueVerbNode))]
	public class RunValueVerbNodeEditor : NodeEditor {

		public override void OnBodyGUI() {
			var node = target as RunValueVerbNode;
			var ioGraph = node.valueVerb as VerbGraph;

			if (ioGraph != null) {
				var needsUpdate = !node.HasMatchingNodeInputsAndOutputsForGraphInputsAndOutputs(ioGraph);
				if (needsUpdate) {
					node.UpdatePorts();
				}
			}

			base.OnBodyGUI();
		}


	}
}