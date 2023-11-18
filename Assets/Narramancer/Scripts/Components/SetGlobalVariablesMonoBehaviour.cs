
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Narramancer {
	[DefaultExecutionOrder(-1)]

	public class SetGlobalVariablesMonoBehaviour : SerializableMonoBehaviour {

		[SerializeField]
		private bool setOnStart = true;

		[SerializeField, HideInInspector]
		private List<VariableAssignment> assignments = new List<VariableAssignment>();
		public static string AssignmentsFieldName => nameof(assignments);

		private void Start() {
			if (setOnStart && !valuesOverwrittenByDeserialize) {
				ApplyValues();
			}
		}

		public void CreateInputs() {
			assignments.MatchToVariables(NarramancerSingleton.Instance.GlobalVariables);
		}

		public void ApplyValues() {

			var blackboard = NarramancerSingleton.Instance.StoryInstance.Blackboard;
			assignments.ApplyAssignmentsToBlackboard(NarramancerSingleton.Instance.GlobalVariables, blackboard);
		}

		public override void Deserialize(StoryInstance map) {
			base.Deserialize(map);

			// apply CERTAIN types of values (that are specifically NOT serialized/desiralized)
			var blackboard = NarramancerSingleton.Instance.StoryInstance.Blackboard;
			foreach (var assignment in assignments) {
				var globalVariable = NarramancerSingleton.Instance.GlobalVariables.FirstOrDefault(x => VariableAssignment.TypeToString(x.Type).Equals(assignment.type, StringComparison.Ordinal) && x.Id.Equals(assignment.id, StringComparison.Ordinal));
				if (globalVariable != null) {
					object value = null;
					switch (assignment.type) {
						case "int":
						case "bool":
						case "float":
						case "string":
							// this area left blank intentionally
							break;
						default:
							value = assignment.objectValue;
							break;
					}
					if (value != null) {
						blackboard.Set(globalVariable.VariableKey, value);
					}

				}
			}
		}
	}

}
