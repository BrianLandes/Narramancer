
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Narramancer {
	[Serializable]
	public class NarramancerPortWithAssignment : NarramancerPort {

		[SerializeField]
		private VariableAssignment assignment = new VariableAssignment();
		public VariableAssignment Assignment => assignment;
		public static string AssignmentFieldName => nameof(assignment);

	}


	public static class NarramancerPortWithAssignmentExtensions {

		public static void ApplyAssignmentsToBlackboard(this List<NarramancerPortWithAssignment> variables, Blackboard blackboard) {

			foreach (var variable in variables) {
				var assignment = variable.Assignment;
				if (variable != null) {
					object value = null;
					switch (assignment.type) {
						case "int":
							value = assignment.intValue;
							break;
						case "bool":
							value = assignment.boolValue;
							break;
						case "float":
							value = assignment.floatValue;
							break;
						case "string":
							value = assignment.stringValue;
							break;
						default:
							value = assignment.objectValue;
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