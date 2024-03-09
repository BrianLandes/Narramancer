
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Narramancer {
	public class RunActionVerbMonoBehaviour : SerializableMonoBehaviour {

		[SerializeField]
		private ActionVerb verb = default;

		[SerializeField]
		private bool runOnStart = true;

		[SerializeMonoBehaviourField]
		private NodeRunner runner = default;
		public NodeRunner Runner => runner;

		[SerializeField, HideInInspector]
		private List<VariableAssignment> assignments = new List<VariableAssignment>();
		public static string AssignmentsFieldName => nameof(assignments);

#if UNITY_EDITOR
		[MenuItem("GameObject/Narramancer/Run Action Verb", false, 10)]
		static void CreateGameObject(MenuCommand menuCommand) {

			GameObject gameObject = new GameObject("Run Action Verb");
			gameObject.AddComponent<RunActionVerbMonoBehaviour>();

			GameObjectUtility.SetParentAndAlign(gameObject, menuCommand.context as GameObject);

			Undo.RegisterCreatedObjectUndo(gameObject, "Create " + gameObject.name);
			Selection.activeObject = gameObject;
		}
#endif



		private void Start() {
			if (!valuesOverwrittenByDeserialize && runOnStart) {
				RunVerb();
			}
		}

		public void CreateInputs() {
			if (verb == null) { return; }

			assignments.MatchToVariables(verb.Inputs);

		}

		public void RunVerb() {

			if (runner == null) {
				runner = NarramancerSingleton.Instance.CreateNodeRunner(gameObject.name + this.GetHashCode());
			}

			if (verb == null) {
				Debug.LogError("Verb was null", this);
				return;
			}

			assignments.ApplyAssignmentsToBlackboard(verb.Inputs, runner.Blackboard);

			if (verb.TryGetFirstRunnableNodeAfterRootNode(out var runnableNode)) {
				runner.Start(runnableNode);
			}

		}

		public override void Deserialize(StoryInstance map) {
			base.Deserialize(map);

			// apply CERTAIN types of values (that are specifically NOT serialized/desiralized)
			if (runner != null) {
				foreach (var assignment in assignments) {
					var globalVariable = verb.Inputs.FirstOrDefault(x => VariableAssignment.TypeToString(x.Type).Equals(assignment.type, StringComparison.Ordinal) && x.Id.Equals(assignment.id, StringComparison.Ordinal));
					if (globalVariable != null) {
						object value = null;
						switch (assignment.type) {
							case "int":
							case "bool":
							case "float":
							case "string":
							case "color":
								// this area left blank intentionally
								break;
							default:
								value = assignment.objectValue;
								break;
						}
						if (value != null) {
							runner.Blackboard.Set(globalVariable.VariableKey, value);
						}

					}
				}
			}

		}

	}
}