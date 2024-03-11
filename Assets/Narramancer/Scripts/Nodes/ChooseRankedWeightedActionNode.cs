
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	[NodeWidth(300)]
	[CreateNodeMenu("Noun/Choose Ranked Weighted Action")]
	public class ChooseRankedWeightedActionNode : AbstractInstanceInputChainedRunnableNode {

		public List<RankedWeightedAction> actions = new List<RankedWeightedAction>();

		[SerializeField]
		[HideInInspector]
		private string log;
		public static string LogFieldName => nameof(log);

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			var instance = GetInstance(runner.Blackboard);
			if (instance == null) {
				Debug.LogError("Instance was null", this);
				return;
			}

			var chosenAction = RankedWeightedAction.Choose(runner.Blackboard, instance, actions, ref log);
			if (chosenAction != null) {
				var effectGraph = chosenAction.GetEffectGraph();
				if (effectGraph == null) {
					Debug.LogError("Graph was null", chosenAction);
					return;
				}

				var inputPort = effectGraph.GetInput<NounInstance>();
				if (inputPort == null) {
					Debug.LogError("Graph did not have the proper input", chosenAction);
					return;
				}

				runner.Blackboard.Set(inputPort.VariableKey, instance);

				if (effectGraph.TryGetFirstRunnableNodeAfterRootNode(out var runnableNode)) {
					runner.Prepend(runnableNode);
				}
			}
			else {
				log += "\nFailed to choose an Action.";
			}

		}

		public override object GetValue(INodeContext context, XNode.NodePort port) {
			if (port.fieldName.Equals(nameof(passThroughInstance))) {
				if (Application.isPlaying) {
					return GetInstance(context);
				}
			}
			return base.GetValue(context, port);
		}
#if UNITY_EDITOR
		public void CreateChildAction() {
			var rankedWeightedAction = PseudoEditorUtilities.CreateAndAddChild<RankedWeightedAction>(this, nameof(RankedWeightedAction).Nicify());
			actions.Add(rankedWeightedAction);
		}
#endif

	}
}