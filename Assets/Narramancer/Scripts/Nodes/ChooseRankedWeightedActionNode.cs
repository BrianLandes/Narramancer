
using System.Collections.Generic;
using System.Linq;
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

				foreach (var (input, portName) in ForwardedInputs(new[] { chosenAction })) {
					var nodePort = GetInputPort(portName);
					if (nodePort != null) {
						input.AssignValueFromNodePort(runner.Blackboard, nodePort);
					}
				}

				if (effectGraph.TryGetFirstRunnableNodeAfterRootNode(out var runnableNode)) {
					runner.Prepend(runnableNode);
				}
			}
			else {
				log += "\nFailed to choose an Action.";
			}

		}

		/// <summary>
		/// One input port per input the actions' effect graphs need beyond the instance this node supplies itself,
		/// so whichever action is chosen runs with its inputs assigned.
		/// </summary>
		public override void UpdatePorts() {
			var keepPorts = new List<NodePort>();
			foreach (var (input, portName) in ForwardedInputs(actions)) {
				keepPorts.Add(this.GetOrAddDynamicInput(input.Type, portName));
			}
			this.ClearDynamicPortsExcept(keepPorts);

			base.UpdatePorts();
		}

		/// <summary>
		/// Every effect-graph input of <paramref name="forActions"/> that needs a port on this node, with the port
		/// it is fed from.
		/// <para>
		/// Port names come from all of the node's actions, not just <paramref name="forActions"/>, so an input
		/// resolves to the same port at run time as when the ports were built. Inputs that share a name and a type
		/// share a port, since they mean the same thing to the author. Inputs that share a name but not a type get
		/// a port each, named with their type — one port cannot carry both.
		/// </para>
		/// </summary>
		public IEnumerable<(NarramancerPort input, string portName)> ForwardedInputs(IEnumerable<RankedWeightedAction> forActions) {
			var all = actions.SelectMany(ExtraEffectInputs).ToList();
			var clashingNames = new HashSet<string>(all
				.GroupBy(input => input.Name)
				.Where(group => group.Select(input => input.Type).Distinct().Count() > 1)
				.Select(group => group.Key));
			var staticPortNames = new HashSet<string>(Ports.Where(port => port.IsStatic).Select(port => port.fieldName));

			foreach (var input in forActions.SelectMany(ExtraEffectInputs)) {
				var portName = input.Name;
				if (clashingNames.Contains(portName) || staticPortNames.Contains(portName)) {
					portName = $"{input.Name} ({input.Type.Name})";
				}
				yield return (input, portName);
			}
		}

		/// <summary>An action's effect-graph inputs, less the instance input this node fills in itself.</summary>
		private static IEnumerable<NarramancerPort> ExtraEffectInputs(RankedWeightedAction action) {
			var effectGraph = action != null ? action.GetEffectGraph() : null;
			if (effectGraph == null) {
				return Enumerable.Empty<NarramancerPort>();
			}
			var instanceInput = effectGraph.GetInput<NounInstance>();
			return effectGraph.Inputs.Where(input => input != instanceInput && input.Type != null);
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