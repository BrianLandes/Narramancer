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

		/// <summary>
		/// Rebuilds <paramref name="assignments"/> to line up with <paramref name="variables"/>, keeping every value
		/// that can still be matched to its variable.
		/// <para>
		/// A match used to need the id <em>and</em> the name, so renaming a verb input — or deleting and re-adding
		/// it, which mints a new id — silently wiped the value assigned to it in every component that used the
		/// verb. Now an assignment follows its variable through either change, as long as the type still
		/// matches, and its stale half is repaired.
		/// </para>
		/// <para>
		/// The matching runs in passes, most confident first, each one choosing only from what the earlier passes
		/// left: exact, then by id, then by name. A single "id or name" check would let a new input that took a
		/// renamed input's old name claim that input's value, depending only on which came first in the list.
		/// </para>
		/// </summary>
		public static void MatchToVariables<T>(this List<VariableAssignment> assignments, List<T> variables) where T : NarramancerPort {
			var unclaimed = assignments.ToList();
			var matched = new VariableAssignment[variables.Count];

			Claim(variables, unclaimed, matched, (assignment, variable) => SameId(assignment, variable) && SameName(assignment, variable));
			Claim(variables, unclaimed, matched, SameId);
			Claim(variables, unclaimed, matched, SameName);

			assignments.Clear();
			for (var ii = 0; ii < variables.Count; ii++) {
				var variable = variables[ii];
				var assignment = matched[ii] ?? new VariableAssignment() {
					type = VariableAssignment.TypeToString(variable.Type),
				};
				assignment.id = variable.Id;
				assignment.name = variable.Name;
				assignments.Add(assignment);
			}
		}

		private static bool SameId(VariableAssignment assignment, NarramancerPort variable) {
			return string.Equals(assignment.id, variable.Id, StringComparison.Ordinal);
		}

		private static bool SameName(VariableAssignment assignment, NarramancerPort variable) {
			return string.Equals(assignment.name, variable.Name, StringComparison.Ordinal);
		}

		private static void Claim<T>(List<T> variables, List<VariableAssignment> unclaimed, VariableAssignment[] matched,
			Func<VariableAssignment, NarramancerPort, bool> matches) where T : NarramancerPort {
			for (var ii = 0; ii < variables.Count; ii++) {
				if (matched[ii] != null) {
					continue;
				}
				var type = VariableAssignment.TypeToString(variables[ii].Type);
				var match = unclaimed.FirstOrDefault(assignment => string.Equals(assignment.type, type, StringComparison.Ordinal) && matches(assignment, variables[ii]));
				if (match != null) {
					matched[ii] = match;
					unclaimed.Remove(match);
				}
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