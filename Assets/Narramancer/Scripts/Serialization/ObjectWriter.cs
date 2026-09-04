using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Narramancer.Serialization.Json;
using UnityEngine;

namespace Narramancer.Serialization {

	/// <summary>
	/// Writes a CLR object graph to a <see cref="JsonValue"/> tree. One instance per serialize call: it
	/// tracks reference identity, so a shared or cyclic object is written once with an <c>$id</c> and
	/// referred to thereafter with a <c>$ref</c>. A <c>$type</c> tag is written only where the runtime
	/// type differs from the slot's declared type — the polymorphic case.
	/// </summary>
	internal sealed class ObjectWriter {

		private readonly IReferenceResolver references;
		private readonly Dictionary<object, int> seen = new Dictionary<object, int>(ReferenceEqualityComparer.Instance);

		private int nextId = 1;

		public ObjectWriter(IReferenceResolver references) {
			this.references = references;
		}

		public JsonValue Write(object value, Type declaredType) {
			if (value == null) {
				return JsonValue.Null;
			}

			// A boxed Nullable<T> is either null (handled above) or a boxed T, so treat the slot as T.
			var underlying = Nullable.GetUnderlyingType(declaredType);
			if (underlying != null) {
				declaredType = underlying;
			}

			// System.Type values arrive as RuntimeType, which has no useful fields to reflect over. They
			// reach a save through SerializableType; the Continuation work is expected to delete the last
			// of them, at which point this branch can go too.
			if (value is Type typeValue) {
				var typeNode = JsonValue.NewObject();
				typeNode.Add(NarramancerSerializer.SystemTypeKey, JsonValue.String(TypeNames.Write(typeValue)));
				return typeNode;
			}

			var runtimeType = value.GetType();
			RefuseCompilerGeneratedType(runtimeType);

			if (Scalars.IsScalar(runtimeType)) {
				if (runtimeType == declaredType) {
					return Scalars.Write(value);
				}
				var envelope = JsonValue.NewObject();
				envelope.Add(NarramancerSerializer.TypeKey, JsonValue.String(TypeNames.Write(runtimeType)));
				envelope.Add(NarramancerSerializer.ValueKey, Scalars.Write(value));
				return envelope;
			}

			if (!runtimeType.IsValueType) {
				// An asset the save points at but does not own is written as an id handle. Inlining one
				// would serialize the asset's own fields into the save — the save would then carry a stale
				// copy of authored content that a later edit could never update.
				if (value is UnityEngine.Object unityObject) {
					return WriteAssetHandle(unityObject);
				}

				if (seen.TryGetValue(value, out var existingId)) {
					var backReference = JsonValue.NewObject();
					backReference.Add(NarramancerSerializer.ReferenceKey, Number(existingId));
					return backReference;
				}

				var id = nextId++;
				seen[value] = id;

				var node = JsonValue.NewObject();
				node.Add(NarramancerSerializer.IdKey, Number(id));
				if (runtimeType != declaredType) {
					node.Add(NarramancerSerializer.TypeKey, JsonValue.String(TypeNames.Write(runtimeType)));
				}
				WriteBody(node, value, runtimeType);
				return node;
			}

			// A non-scalar struct: no identity to track, but it may still need a type tag.
			var structNode = JsonValue.NewObject();
			if (runtimeType != declaredType) {
				structNode.Add(NarramancerSerializer.TypeKey, JsonValue.String(TypeNames.Write(runtimeType)));
			}
			WriteBody(structNode, value, runtimeType);
			return structNode;
		}

		/// <summary>
		/// Refuses to write a compiler-generated type — a lambda's closure class, an iterator state machine,
		/// an anonymous type.
		/// <para>
		/// These used to be written, and that was the bug: the ordinal in <c>&lt;&gt;c__DisplayClass22_0</c> is
		/// assigned by the compiler and moves when the source file is edited, so a save could stop resolving
		/// its own continuations after an unrelated change. <c>Continuation</c> replaced all of them.
		/// </para>
		/// <para>
		/// Failing here rather than simply not having a carve-out for them: without one, a closure's fields
		/// are neither public nor <c>[SerializeField]</c>, so it would serialize as an <em>empty</em> object
		/// and silently lose its captured state. A loud failure is the only version of this that cannot
		/// quietly reintroduce the bug.
		/// </para>
		/// </summary>
		private static void RefuseCompilerGeneratedType(Type type) {
			if (type.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: false)) {
				throw new SerializationException(
					$"'{type}' is compiler-generated — a lambda closure or similar — and cannot be saved: its name contains an ordinal that changes when the source file is edited. " +
					$"Use a {nameof(Continuation)} instead, or {nameof(Promise)}.WhenDoneThisSession for a callback that does not need to survive a save.");
			}
		}

		private JsonValue WriteAssetHandle(UnityEngine.Object unityObject) {
			// A destroyed or missing asset is Unity's "fake null": not a C# null reference, but nothing a save
			// can point at. Writing it as JSON null is the honest answer, and it reads back as a plain null.
			if (unityObject == null) {
				return JsonValue.Null;
			}

			if (references == null) {
				throw new SerializationException(
					$"The graph points at the Unity object '{unityObject.name}' ({unityObject.GetType()}), but this serializer has no {nameof(IReferenceResolver)}. " +
					"Construct it with one, or the reference cannot be given a stable id.");
			}

			// Deliberately not fatal, and deliberately asymmetric with the reader.
			//
			// Here we know exactly what is missing and can name it, and the author has three ways to fix it. On
			// read we know only that an id did not resolve, which is indistinguishable from an id that resolves
			// to the *wrong* thing — the silent corruption the GUID work exists to remove — so that stays fatal.
			// Losing one blackboard entry also beats losing the save: throwing here would fail at the worst
			// possible moment in a shipped game, when the player pressed Save.
			if (!references.TryGetId(unityObject, out var id)) {
				Debug.LogError(
					$"Narramancer cannot save a reference to '{unityObject.name}' ({unityObject.GetType().Name}) — it has no stable id, so the reference is being dropped. " +
					"Fix it in one of three ways: move the asset under a Resources folder, add it to the Narramancer asset manifest's Additional Assets, " +
					$"or call {nameof(RuntimeAssetRegistry)}.{nameof(RuntimeAssetRegistry.Register)} for it at runtime.",
					unityObject);
				return JsonValue.Null;
			}

			var node = JsonValue.NewObject();
			node.Add(NarramancerSerializer.AssetKey, JsonValue.String(id));
			return node;
		}

		private void WriteBody(JsonValue node, object value, Type runtimeType) {
			if (runtimeType.IsArray) {
				WriteElements(node, (IEnumerable)value, runtimeType.GetElementType());
				return;
			}

			if (CollectionInfo.TryList(runtimeType, out var elementType)) {
				WriteElements(node, (IEnumerable)value, elementType);
				return;
			}

			if (CollectionInfo.TryDictionary(runtimeType, out var keyType, out var valueType)) {
				var entries = JsonValue.NewArray();
				foreach (DictionaryEntry entry in (IDictionary)value) {
					var pair = JsonValue.NewObject();
					pair.Add(NarramancerSerializer.EntryKeyKey, Write(entry.Key, keyType));
					pair.Add(NarramancerSerializer.EntryValueKey, Write(entry.Value, valueType));
					entries.Add(pair);
				}
				node.Add(NarramancerSerializer.EntriesKey, entries);
				return;
			}

			foreach (var field in SerializationFields.Of(runtimeType)) {
				node.Add(field.Name, Write(field.GetValue(value), field.FieldType));
			}
		}

		private void WriteElements(JsonValue node, IEnumerable items, Type elementType) {
			var values = JsonValue.NewArray();
			foreach (var item in items) {
				values.Add(Write(item, elementType));
			}
			node.Add(NarramancerSerializer.ValuesKey, values);
		}

		private static JsonValue Number(int value) => JsonValue.Number(value.ToString(CultureInfo.InvariantCulture));
	}
}
