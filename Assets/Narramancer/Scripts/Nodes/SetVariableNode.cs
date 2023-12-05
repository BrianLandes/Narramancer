
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
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
		private string scene = "";
		public static string SceneFieldName => nameof(scene);
		public string Scene => scene;

		[SerializeField]
		private string variableId = "";
		public static string VariableIdFieldName => nameof(variableId);

		[SerializeField]
		private string variableName = "";
		public static string VariableNameFieldName => nameof(variableName);

		[SerializeField]
		private string variableKey = "";
		public static string VariableKeyFieldName => nameof(variableKey);

		public static string PORT_NAME = "value";

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			var outputNodePort = GetInputPort(PORT_NAME);
			if (outputNodePort == null) {
				return;
			}
			var inputValue = outputNodePort.GetInputValue(runner.Blackboard);

			switch (scope) {
				case ScopeType.Scene:
				case ScopeType.Global:
					NarramancerSingleton.Instance.StoryInstance.Blackboard.Set(variableKey, inputValue);
					break;
				case ScopeType.Verb:
					runner.Blackboard.Set(variableKey, inputValue);
					break;
			}
			

		}

		public string GetVariableLabel() {
			switch (scope) {
				default:
				case ScopeType.Scene:
					return $"{scene}.{variableName}";
				case ScopeType.Global:
					return $"Global.{variableName}";
				case ScopeType.Verb:
					return $"{graph.name}.{variableName}";
			}
		}

		public bool IsSceneScopeAndCurrentSceneIsNotLoaded() {
			return scope == ScopeType.Scene && !SceneManager.GetSceneByName(scene).isLoaded;
		}

		public List<NarramancerPort> GetScopeVariables() {
			switch (scope) {
				default:
				case ScopeType.Scene:
					var narramancerScene = GameObject.FindAnyObjectByType<NarramancerScene>();
					if (narramancerScene == null) {
						return Array.Empty<NarramancerPort>().ToList();
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

		private NarramancerPort GetVariable() {
			if (variableId.IsNullOrEmpty()) {
				return null;
			}
			var variables = GetScopeVariables();
			if (variables == null) {
				return null;
			}
			return variables.FirstOrDefault(x => x.Id.Equals(variableId));
		}

		public void SetVariable(ScopeType scope, NarramancerPort outputPort) {
			this.scope = scope;
			variableId = outputPort.Id;
			variableName = outputPort.Name;
			variableKey = outputPort.VariableKey;
			ApplyChanges();
		}

		public void ApplyChanges() {

			var output = GetVariable();

			if (output == null) {
				ClearDynamicPorts();
				return;
			}

			var nodePorts = new List<NodePort>();

			var objectType = output.Type;

			var outputPort = this.GetOrAddDynamicInput(objectType, PORT_NAME);
			nodePorts.Add(outputPort);

			this.ClearDynamicPortsExcept(nodePorts);

			if (scope==ScopeType.Scene) {
				var narramancerScene = GameObject.FindAnyObjectByType<NarramancerScene>();
				if (narramancerScene != null) {
					scene = narramancerScene.gameObject.scene.name;
				}
			}
		}

	}
}