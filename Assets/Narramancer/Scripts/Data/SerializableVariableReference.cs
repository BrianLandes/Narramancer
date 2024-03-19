using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using XNode;

namespace Narramancer {

	[Serializable]
	public enum InputOrOutput {
		Input,
		Output
	}

	[Serializable]
	public class SerializableVariableReference {

		[Serializable]
		public enum ScopeType {
			Scene,
			Global,
			Verb
		}
		[SerializeField]
		private ScopeType scope = ScopeType.Scene;
		public static string ScopeFieldName => nameof(scope);
		public ScopeType Scope => scope;

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
		public string Key => variableKey;

		[SerializeField]
		private InputOrOutput inputOrOutput = InputOrOutput.Input;
		public bool IsInput => inputOrOutput == InputOrOutput.Input;
		public bool IsOutput => inputOrOutput == InputOrOutput.Output;

		public static string PORT_NAME = "value";

		public SerializableVariableReference() { }

		public SerializableVariableReference(InputOrOutput inputOrOutput) {
			this.inputOrOutput = inputOrOutput;
		}

		public Blackboard GetBlackboard(Blackboard verbBlackboard) {

			switch (scope) {
				default:
				case ScopeType.Scene:
				case ScopeType.Global:
					return NarramancerSingleton.Instance.StoryInstance.Blackboard;

				case ScopeType.Verb:
					return verbBlackboard;
			}
		}

		public string GetVariableLabel(NodeGraph nodeGraph) {
			switch (scope) {
				default:
				case ScopeType.Scene:
					return $"{scene}.{variableName}";
				case ScopeType.Global:
					return $"Global.{variableName}";
				case ScopeType.Verb:
					return $"{nodeGraph.name}.{variableName}";
			}
		}

		public bool IsSceneScopeAndCurrentSceneIsNotLoaded() {
			return scope == ScopeType.Scene && !SceneManager.GetSceneByName(scene).isLoaded;
		}

		public List<NarramancerPort> GetScopeVariables(VerbGraph verbGraph) {
			switch (scope) {
				default:
				case ScopeType.Scene:
					var narramancerScene = GameObjectExtensions.FindAnyObjectByType<NarramancerScene>();
					if (narramancerScene == null) {
						return Array.Empty<NarramancerPort>().ToList();
					}
					return narramancerScene.Variables.Cast<NarramancerPort>().ToList();
				case ScopeType.Global:
					return NarramancerSingleton.Instance.GlobalVariables.Cast<NarramancerPort>().ToList();
				case ScopeType.Verb:
					switch (inputOrOutput) {
						default:
						case InputOrOutput.Input:
							return verbGraph.Inputs.Cast<NarramancerPort>().ToList();
						case InputOrOutput.Output:
							return verbGraph.Outputs.Cast<NarramancerPort>().ToList();
					}
			}
		}

		public NarramancerPort GetVariable(VerbGraph verbGraph) {
			if (variableId.IsNullOrEmpty()) {
				return null;
			}
			var variables = GetScopeVariables(verbGraph);
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
		}

		public void UpdateScene() {
			if (scope == ScopeType.Scene) {
				var narramancerScene = GameObjectExtensions.FindAnyObjectByType<NarramancerScene>();
				if (narramancerScene != null) {
					scene = narramancerScene.gameObject.scene.name;
				}
			}
		}
	}
}
