using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Narramancer {

	[Serializable]
	public class NarramancerPortWithAssignmentList {
		[SerializeField]
		public List<NarramancerPortWithAssignment> list = new List<NarramancerPortWithAssignment>();
	}

	public class NarramancerScene : MonoBehaviour {



		[SerializeField]
		private NarramancerPortWithAssignmentList variables = new NarramancerPortWithAssignmentList();
		public static string VariablesFieldName => nameof(variables);
		public List<NarramancerPortWithAssignment> Variables => variables.list;


#if UNITY_EDITOR
		[MenuItem("GameObject/Narramancer/NarramancerScene", false, 10)]
		static void CreateGameObject(MenuCommand menuCommand) {

			GameObject gameObject = new GameObject("Narramancer Scene");
			gameObject.AddComponent<NarramancerScene>();

			GameObjectUtility.SetParentAndAlign(gameObject, menuCommand.context as GameObject);

			Undo.RegisterCreatedObjectUndo(gameObject, "Create " + gameObject.name);
			Selection.activeObject = gameObject;
		}
#endif

		private void Start() {

			variables.list.ApplyAssignmentsToBlackboard(NarramancerSingleton.Instance.StoryInstance.Blackboard);
		}
	}
}
