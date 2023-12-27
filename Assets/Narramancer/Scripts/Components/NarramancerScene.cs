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

				foreach (var verb in runOnStartVerbs.list) {
					if (verb.TryGetFirstRunnableNodeAfterRootNode(out var runnableNode)) {
						var runner = NarramancerSingleton.Instance.CreateNodeRunner(verb.name);
						runner.Start(runnableNode);
						runner.name = verb.name;
						nodeRunners.Add(runner);
					}
				}
			}

		}

	}
}
