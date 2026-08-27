using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Narramancer.Serialization {

	/// <summary>
	/// Chooses which fields of a type are serialized, walking the whole inheritance chain so private
	/// fields declared on a base class are included.
	/// <para>
	/// The policy deliberately mirrors OdinSerializer's Unity policy, because that is what wrote every
	/// save that exists today: <b>an instance field is serialized when it is public or carries
	/// <see cref="SerializeField"/>, unless it carries <see cref="NonSerializedAttribute"/>.</b>
	/// Getting this wrong is the most dangerous mistake available in the port — v1 marks nearly all of
	/// its save state as <c>[SerializeField] private</c>, so a public-fields-only policy would skip
	/// almost the entire save and still produce a payload that parses.
	/// </para>
	/// <para>
	/// <see cref="NonSerializedAttribute"/> is load-bearing in v1 and must keep working:
	/// <c>Blackboard.gameObjects</c>, <c>Blackboard.components</c> and <c>NodeRunner.lastRunningNode</c>
	/// are all deliberately excluded from saves.
	/// </para>
	/// </summary>
	internal static class SerializationFields {

		private static readonly Dictionary<Type, FieldInfo[]> cache = new Dictionary<Type, FieldInfo[]>();

		public static FieldInfo[] Of(Type type) {
			if (cache.TryGetValue(type, out var cached)) {
				return cached;
			}

			var fields = new List<FieldInfo>();
			for (var current = type; current != null && current != typeof(object); current = current.BaseType) {

				// A compiler-generated closure class captures its locals in fields that are neither public
				// nor attributed, so the general policy would skip all of them. Odin has the same carve-out,
				// and `Promise.doneCallbacks` depends on it until the Continuation work lands and closures
				// stop being serialized at all — see docs/CONTINUATION_DESIGN.md.
				var isClosure = current.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false);

				var declared = current.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
				foreach (var field in declared) {
					if (field.IsNotSerialized) {
						continue;
					}
					if (isClosure || field.IsPublic || field.IsDefined(typeof(SerializeField), inherit: false)) {
						fields.Add(field);
					}
				}
			}

			var result = fields.ToArray();
			cache[type] = result;
			return result;
		}

		/// <summary>Clears the field cache. Tests only — the policy is fixed at runtime.</summary>
		internal static void ClearCache() => cache.Clear();
	}
}
