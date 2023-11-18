
using XNodeEditor;

namespace Narramancer {
	[CustomNodeEditor(typeof(RunActionVerbNode))]
	public class RunActionVerbNodeEditor : ChainedRunnableNodeEditor {

		public override void OnBodyGUI() {
			var runActionVerbNode = target as RunActionVerbNode;
			var ioGraph = runActionVerbNode.actionVerb as VerbGraph;
			if (ioGraph != null) {
				var needsUpdate = !runActionVerbNode.HasMatchingNodeInputsAndOutputsForGraphInputsAndOutputs(ioGraph);
				if (needsUpdate) {
					runActionVerbNode.UpdatePorts();
				}
			}

			base.OnBodyGUI();
		}
	}
}