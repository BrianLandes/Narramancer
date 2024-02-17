
using System.Linq;
using XNodeEditor;

namespace Narramancer {
	[CustomNodeEditor(typeof(AnyInstancePassesPredicateNode))]
	public class AnyInstancePassesPredicateNodeEditor : NodeEditor {

		public override void OnBodyGUI() {
			var node = target as AnyInstancePassesPredicateNode;
			var ioGraph = node.Predicate as VerbGraph;

			if (ioGraph != null) {
				var needsUpdate = !HasMatchingNodeInputsGraphInputs(node, ioGraph);
				if (needsUpdate) {
					node.RebuildPorts();
				}
			}
			
			base.OnBodyGUI();
		}

		/// <summary>
		/// Needed a custom validation method because the node handles the TypicalInstance input manually.
		/// </summary>
		private bool HasMatchingNodeInputsGraphInputs(AnyInstancePassesPredicateNode node, VerbGraph graph) {
			if (graph.Inputs.Count != node.DynamicInputs.Count() - 1) {
				return false;
			}

			var characterInputPort = graph.GetInput<NounInstance>();

			bool NodeHasGraphPort(NarramancerPort port) {
				if (port == characterInputPort) {
					return true;
				}
				var nodePort = node.GetInputPort(port.Name);
				if (nodePort == null) {
					return false;
				}
				if (nodePort.ValueType != port.Type) {
					return false;
				}
				return true;
			}

			return graph.Inputs.Any(graphInput => !NodeHasGraphPort(graphInput));
		}

	}
}