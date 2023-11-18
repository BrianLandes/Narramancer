using System;
using System.Collections.Generic;
using System.Linq;

namespace Narramancer {

	[Serializable]
	public class VariableAssignment {
		public string name = "value";
		public string id;
		public string type;
		public bool boolValue;
		public int intValue;
		public float floatValue;
		public string stringValue;
		public UnityEngine.Object objectValue;


		public static string TypeToString(Type type) {
			if (typeof(int) == type) {
				return "int";
			}
			if (typeof(float) == type) {
				return "float";
			}
			if (typeof(bool) == type) {
				return "bool";
			}
			if (typeof(string) == type) {
				return "string";
			}
			return type.AssemblyQualifiedName;
		}

		public static string TypeNameToVariableAssignmentType(string typeAssemblyQualifiedName) {
			if (typeof(int).AssemblyQualifiedName.Equals(typeAssemblyQualifiedName, StringComparison.Ordinal)) {
				return "int";
			}
			if (typeof(float).AssemblyQualifiedName.Equals(typeAssemblyQualifiedName, StringComparison.Ordinal)) {
				return "float";
			}
			if (typeof(bool).AssemblyQualifiedName.Equals(typeAssemblyQualifiedName, StringComparison.Ordinal)) {
				return "bool";
			}
			if (typeof(string).AssemblyQualifiedName.Equals(typeAssemblyQualifiedName, StringComparison.Ordinal)) {
				return "string";
			}
			return typeAssemblyQualifiedName;
		}
	}

	public static class VariableAssignmentExtensions {

		public static void MatchToVariables<T>(this List<VariableAssignment> assignments, List<T> variables) where T: NarramancerPort {
			var existingAssignments = assignments.ToList();

			assignments.Clear();

			foreach (var variable in variables) {

				var existingAssignment = existingAssignments.FirstOrDefault(x => x.id.Equals(variable.Id, StringComparison.Ordinal)
					&& x.name.Equals(variable.Name, StringComparison.Ordinal)
					&& VariableAssignment.TypeToString(variable.Type).Equals(x.type, StringComparison.Ordinal));

				if (existingAssignment != null) {
					assignments.Add(existingAssignment);
					continue;
				}
				var newAssignment = new VariableAssignment() {
					name = variable.Name,
					id = variable.Id,
					type = VariableAssignment.TypeToString(variable.Type),
				};
				assignments.Add(newAssignment);
			}
		}

		public static void ApplyAssignmentsToBlackboard<T>(this List<VariableAssignment> assignments, List<T> variables, Blackboard blackboard) where T: NarramancerPort {

			foreach (var assignment in assignments) {
				var globalVariable = variables.FirstOrDefault(x => VariableAssignment.TypeToString(x.Type).Equals(assignment.type, StringComparison.Ordinal) && x.Id.Equals(assignment.id, StringComparison.Ordinal));
				if (globalVariable != null) {
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
						blackboard.Set(globalVariable.VariableKey, value);
					}

				}
			}
		}
	}
}