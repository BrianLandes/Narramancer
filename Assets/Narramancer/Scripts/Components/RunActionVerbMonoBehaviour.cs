
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Narramancer {
	public class RunActionVerbMonoBehaviour : SerializableMonoBehaviour {

		[SerializeField]
		private ActionVerb verb = default;

		[SerializeField]
		private bool runOnStart = true;

		[SerializeMonoBehaviourField]
		private NodeRunner runner = default;

		[SerializeField, HideInInspector]
		private List<VariableAssignment> assignments = new List<VariableAssignment>();
		public static string AssignmentsFieldName => nameof(assignments);

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
				runner = NarramancerSingleton.Instance.CreateNodeRunner(gameObject.name);
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