using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Narramancer {

	/// <summary>
	/// Saves and restores the <see cref="SaveAttribute"/> fields of one object. Shared by
	/// <see cref="Saveable"/>, for behaviour components, and <see cref="SerializableMonoBehaviour"/>, which still
	/// saves its own fields.
	/// </summary>
	public static class SavedFields {

		private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;

		/// <summary>Reflection is slow and a type's fields never change at runtime, so each type is scanned once.</summary>
		private static readonly Dictionary<Type, FieldInfo[]> fieldsByType = new Dictionary<Type, FieldInfo[]>();

		public static FieldInfo[] Of(Type type) {
			if (!fieldsByType.TryGetValue(type, out var fields)) {
				fields = type.GetFields(Flags).Where(field => field.IsDefined(typeof(SaveAttribute), true)).ToArray();
				fieldsByType[type] = fields;
			}
			return fields;
		}

		public static bool HasAny(Type type) => Of(type).Length > 0;

		/// <param name="key">Where a field is filed, given its name — the owner decides, so keys stay tied to its identity.</param>
		public static void Capture(object owner, Func<string, string> key, StoryInstance story) {
			foreach (var field in Of(owner.GetType())) {
				if (field.FieldType == typeof(Promise)) {
					var value = (Promise)field.GetValue(owner);
					if (value != null) {
						story.Promises[key(field.Name)] = value;
					}
				}
				else
				if (field.FieldType == typeof(NodeRunner)) {
					var value = (NodeRunner)field.GetValue(owner);
					if (value != null) {
						story.NodeRunners[key(field.Name)] = value;
					}
				}
				else {
					story.SaveTable.Set(key(field.Name), field.GetValue(owner), field.FieldType);
				}
			}
		}

		public static void Restore(object owner, Func<string, string> key, StoryInstance story) {
			foreach (var field in Of(owner.GetType())) {
				if (field.FieldType == typeof(Promise)) {
					if (story.Promises.TryGetValue(key(field.Name), out var promise)) {
						field.SetValue(owner, promise);
					}
				}
				else
				if (field.FieldType == typeof(NodeRunner)) {
					if (story.NodeRunners.TryGetValue(key(field.Name), out var runner)) {
						field.SetValue(owner, runner);
					}
				}
				else
				if (story.SaveTable.Contains(key(field.Name), field.FieldType)) {
					// A field added since the save was written keeps its initializer rather than its type's default.
					field.SetValue(owner, story.SaveTable.GetAndRemove(key(field.Name), field.FieldType));
				}
			}
		}
	}
}
