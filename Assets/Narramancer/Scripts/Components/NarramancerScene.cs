using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Narramancer {

	[DefaultExecutionOrder(-100)]
	public class NarramancerScene : SerializableMonoBehaviour {


		[SerializeField]
		NounScriptableObjectList nouns = new NounScriptableObjectList();
		public List<NounScriptableObject> Nouns => nouns.list;
		public static string NounsFieldName => nameof(nouns);


		[SerializeField]
		private NarramancerPortWithAssignmentList variables = new NarramancerPortWithAssignmentList();
		public static string VariablesFieldName => nameof(variables);
		public List<NarramancerPortWithAssignment> Variables => variables.list;


		[SerializeField]
		private ActionVerbList runOnStartVerbs = new ActionVerbList();
		public static string RunOnStartVerbs => nameof(runOnStartVerbs);

		[SerializeMonoBehaviourField]
		private List<NodeRunner> nodeRunners = new List<NodeRunner>();
		public List<NodeRunner> NodeRunners => nodeRunners;

#if UNITY_EDITOR
		[MenuItem("GameObject/Narramancer/Narramancer Scene", false, 10)]
		static void CreateGameObject(MenuCommand menuCommand) {

			GameObject gameObject = new GameObject("Narramancer Scene");
			gameObject.AddComponent<NarramancerScene>();

			GameObjectUtility.SetParentAndAlign(gameObject, menuCommand.context as GameObject);

			Undo.RegisterCreatedObjectUndo(gameObject, "Create " + gameObject.name);
			Selection.activeObject = gameObject;
		}
#endif

		private void Start() {
			if (!valuesOverwrittenByDeserialize) {
				variables.list.ApplyAssignmentsToBlackboard(NarramancerSingleton.Instance.StoryInstance.Blackboard);

				var newInstances = new List<NounInstance>();

				foreach (var typicalNoun in nouns.list) {
					var instance = NarramancerSingleton.Instance.GetInstance(typicalNoun);
					if (instance == null) {
						instance = NarramancerSingleton.Instance.CreateInstance(typicalNoun);
						newInstances.Add(instance);
					}
				}

				// Establish Relationships
				// Must occur after all initial instances have been created
				foreach (var instance in newInstances) {
					instance.AddRelationships(instance.Noun.Relationships, newInstances);
				}

				foreach (var verb in runOnStartVerbs.list) {
					if (verb.TryGetFirstRunnableNodeAfterRootNode(out var runnableNode)) {
						var runner = NarramancerSingleton.Instance.CreateNodeRunner(verb.name);
						runner.Start(runnableNode);
						runner.name = verb.name;
						nodeRunners.Add(runner);
					}
				}
			}
			else {
				// apply CERTAIN types of values (that are specifically NOT serialized/desiralized)
				var blackboard = NarramancerSingleton.Instance.StoryInstance.Blackboard;
				foreach (var variable in variables.list) {
					if (variable != null) {
						object value = null;
						switch (variable.Assignment.type) {
							case "int":
							case "bool":
							case "float":
							case "string":
							case "color":
							case "vector2":
							case "vector3":
								// this area left blank intentionally
								break;
							default:
								value = variable.Assignment.objectValue;
								break;
						}
						if (value != null) {
							blackboard.Set(variable.VariableKey, value);
						}

					}
				}
			}

		}

	}
}
