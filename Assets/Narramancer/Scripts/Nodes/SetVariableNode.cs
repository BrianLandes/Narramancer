
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("Variable/Set Variable")]
	public class SetVariableNode : ChainedRunnableNode {

		[Serializable]
		public enum ScopeType {
			Scene,
			Global,
			Verb
		}
		[SerializeField]
		private ScopeType scope = ScopeType.Scene;
		public static string ScopeFieldName => nameof(scope);

		[SerializeField]
		private string variableId = "";
		public static string VariableIdFieldName => nameof(variableId);

		public static string PORT_NAME = "value";

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			var outputPort = GetOutputPort();

			if (outputPort == null) {
				return;
			}

			var outputNodePort = GetInputPort(PORT_NAME);
			if (outputNodePort == null) {
				return;
			}
			var inputValue = outputNodePort.GetInputValue(runner.Blackboard);

			switch (scope) {
				case ScopeType.Scene:
				case ScopeType.Global:
					NarramancerSingleton.Instance.StoryInstance.Blackboard.Set(outputPort.VariableKey, inputValue);
					break;
				case ScopeType.Verb:
					runner.Blackboard.Set(outputPort.VariableKey, inputValue);
					break;
			}
			

		}

		public List<NarramancerPort> GetScopeVariables() {
			switch (scope) {
				default:
				case ScopeType.Scene:
					var narramancerScene = GameObject.FindAnyObjectByType<NarramancerScene>();
					if (narramancerScene == null) {
						return null;
					}
					return narramancerScene.Variables.Cast<NarramancerPort>().ToList();
				case ScopeType.Global:
					return NarramancerSingleton.Instance.GlobalVariables.Cast<NarramancerPort>().ToList();
				case ScopeType.Verb:
					var verbGraph = graph as VerbGraph;
					if (verbGraph == null) {
						return null;
					}
					return verbGraph.Outputs;

			}
		}

		private NarramancerPort GetOutputPort() {
			if (variableId.IsNullOrEmpty()) {
				return null;
			}
			var variables = GetScopeVariables();
			if (variables == null) {
				return null;
			}
			return variables.FirstOrDefault(x => x.Id.Equals(variableId));
		}

		public void SetOutput(ScopeType scope, NarramancerPort outputPort) {
			this.scope = scope;
			variableId = outputPort.Id;
			ApplyChanges();
		}

		public void ApplyChanges() {

			var output = GetOutputPort();

			if (output == null) {
				ClearDynamicPorts();
				return;
			}

			var nodePorts = new List<NodePort>();

			var objectType = output.Type;

			var outputPort = this.GetOrAddDynamicInput(objectType, PORT_NAME);
			nodePorts.Add(outputPort);

			this.ClearDynamicPortsExcept(nodePorts);

		}

	}
}