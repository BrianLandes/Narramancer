using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
		public Color color;
		public Vector2 vector2;
		public Vector3 vector3;
		public UnityEngine.Object objectValue;

		public object GetValue() {
			switch (type) {
				case "int":
					return intValue;
				case "bool":
					return boolValue;
				case "float":
					return floatValue;
				case "string":
					return stringValue;
				case nameof(color):
					return color;
				case nameof(vector2):
					return vector2;
				case nameof(vector3):
					return vector3;
				default:
					return objectValue;
			}
		}

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
			if (typeof(Color) == type) {
				return nameof(color);
			}
			if (typeof(Vector2) == type) {
				return nameof(vector2);
			}
			if (typeof(Vector3) == type) {
				return nameof(vector3);
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
			if (typeof(Color).AssemblyQualifiedName.Equals(typeAssemblyQualifiedName, StringComparison.Ordinal)) {
				return nameof(color);
			}
			if (typeof(Vector2).AssemblyQualifiedName.Equals(typeAssemblyQualifiedName, StringComparison.Ordinal)) {
				return nameof(vector2);
			}
			if (typeof(Vector3).AssemblyQualifiedName.Equals(typeAssemblyQualifiedName, StringComparison.Ordinal)) {
				return nameof(vector3);
			}
			return typeAssemblyQualifiedName;
		}
	}

	public static class VariableAssignmentExtensions {

		public static void MatchToVariables<T>(this List<VariableAssignment> assignments, List<T> variables) where T : NarramancerPort {
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

		public static void ApplyAssignmentsToBlackboard<T>(this List<VariableAssignment> assignments, List<T> variables, Blackboard blackboard) where T : NarramancerPort {

			foreach (var assignment in assignments) {
				var globalVariable = variables.FirstOrDefault(x => VariableAssignment.TypeToString(x.Type).Equals(assignment.type, StringComparison.Ordinal) && x.Id.Equals(assignment.id, StringComparison.Ordinal));
				if (globalVariable != null) {
					object value = assignment.GetValue();
					if (value != null) {
						blackboard.Set(globalVariable.VariableKey, value);
					}

				}
			}
		}
	}
}