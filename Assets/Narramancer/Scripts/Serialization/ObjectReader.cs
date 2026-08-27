using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Narramancer.Serialization.Json;

namespace Narramancer.Serialization {

	/// <summary>
	/// Rebuilds a CLR object graph from a <see cref="JsonValue"/> tree. One instance per deserialize
	/// call: it maps each <c>$id</c> to the object it created so <c>$ref</c> back-links resolve,
	/// registering an instance <em>before</em> populating it so cycles terminate.
	/// </summary>
	internal sealed class ObjectReader {

		private readonly IReferenceResolver references;
		private readonly Dictionary<int, object> byId = new Dictionary<int, object>();

		public ObjectReader(IReferenceResolver references) {
			this.references = references;
		}

		public object Read(JsonValue node, Type declaredType) {
			// Read a Nullable<T> slot as T: a null node yields null, anything else a boxed T, which
			// reflection assigns straight into the Nullable field.
			var underlying = Nullable.GetUnderlyingType(declaredType);
			if (underlying != null) {
				declaredType = underlying;
			}

			switch (node.Kind) {
				case JsonKind.Null:
					return null;
				case JsonKind.Bool:
				case JsonKind.Number:
				case JsonKind.String:
					if (!Scalars.IsScalar(declaredType)) {
						throw new SerializationException($"The save holds a bare JSON value where a '{declaredType}' was expected.");
					}
					return Scalars.Read(node, declaredType);
				case JsonKind.Array:
					throw new SerializationException($"The save holds a bare JSON array where a '{declaredType}' was expected.");
			}

			if (node.TryGet(NarramancerSerializer.ReferenceKey, out var backReferenceNode)) {
				var backReferenceId = ParseId(backReferenceNode);
				if (byId.TryGetValue(backReferenceId, out var target)) {
					return target;
				}
				throw new SerializationException($"Unresolved {NarramancerSerializer.ReferenceKey} {backReferenceId}.");
			}

			if (node.TryGet(NarramancerSerializer.SystemTypeKey, out var systemTypeNode)) {
				return TypeNames.Read(systemTypeNode.AsString);
			}

			if (node.TryGet(NarramancerSerializer.AssetKey, out var assetNode)) {
				return ReadAssetHandle(assetNode.AsString, declaredType);
			}

			var type = declaredType;
			if (node.TryGet(NarramancerSerializer.TypeKey, out var typeNode)) {
				type = TypeNames.Read(typeNode.AsString);
			}

			if (Scalars.IsScalar(type) && node.TryGet(NarramancerSerializer.ValueKey, out var scalarNode)) {
				return Scalars.Read(scalarNode, type);
			}

			var hasId = node.TryGet(NarramancerSerializer.IdKey, out var idNode);
			var id = hasId ? ParseId(idNode) : -1;

			if (type.IsArray) {
				return ReadArray(node, type.GetElementType(), hasId, id);
			}
			if (CollectionInfo.TryList(type, out var elementType)) {
				return ReadList(node, type, elementType, hasId, id);
			}
			if (CollectionInfo.TryDictionary(type, out var keyType, out var valueType)) {
				return ReadDictionary(node, type, keyType, valueType, hasId, id);
			}
			return ReadObject(node, type, hasId, id);
		}

		private object ReadAssetHandle(string id, Type declaredType) {
			if (references == null) {
				throw new SerializationException($"The save refers to the asset '{id}', but this serializer has no {nameof(IReferenceResolver)} to resolve it with.");
			}

			// Deliberately fatal rather than null. A reference that cannot be resolved means the save no
			// longer describes the project it was written against, and the observed failure mode for a
			// wrong-but-plausible reference is a story that resumes at the wrong node with no error at
			// all. Refusing the load is the only outcome the player can act on.
			if (!references.TryResolve(id, declaredType, out var asset)) {
				throw new SerializationException($"The save refers to the asset '{id}' (expected a '{declaredType}'), which could not be found in this project.");
			}
			return asset;
		}

		private object ReadArray(JsonValue node, Type elementType, bool hasId, int id) {
			node.TryGet(NarramancerSerializer.ValuesKey, out var valuesNode);
			var count = valuesNode?.Items.Count ?? 0;
			var array = Array.CreateInstance(elementType, count);
			if (hasId) {
				byId[id] = array;
			}
			for (var i = 0; i < count; i++) {
				array.SetValue(Read(valuesNode.Items[i], elementType), i);
			}
			return array;
		}

		private object ReadList(JsonValue node, Type type, Type elementType, bool hasId, int id) {
			var list = (IList)Construct(type);
			if (hasId) {
				byId[id] = list;
			}
			if (node.TryGet(NarramancerSerializer.ValuesKey, out var valuesNode)) {
				foreach (var item in valuesNode.Items) {
					list.Add(Read(item, elementType));
				}
			}
			return list;
		}

		private object ReadDictionary(JsonValue node, Type type, Type keyType, Type valueType, bool hasId, int id) {
			var dictionary = (IDictionary)Construct(type);
			if (hasId) {
				byId[id] = dictionary;
			}
			if (node.TryGet(NarramancerSerializer.EntriesKey, out var entriesNode)) {
				foreach (var entry in entriesNode.Items) {
					entry.TryGet(NarramancerSerializer.EntryKeyKey, out var keyNode);
					entry.TryGet(NarramancerSerializer.EntryValueKey, out var valueNode);
					var key = Read(keyNode, keyType);
					if (key == null) {
						throw new SerializationException($"The save holds a null key in a '{type}'.");
					}
					dictionary[key] = Read(valueNode, valueType);
				}
			}
			return dictionary;
		}

		private object ReadObject(JsonValue node, Type type, bool hasId, int id) {
			if (type.IsAbstract || type.IsInterface) {
				throw new SerializationException($"Cannot reconstruct '{type}' — it is abstract. The save is missing the {NarramancerSerializer.TypeKey} tag naming the concrete type.");
			}

			var instance = Allocate(type);

			// Register before populating, so a cyclic reference back to this object resolves to it rather
			// than recursing forever. NodeRunner ↔ Promise ↔ the runner table is exactly such a cycle.
			if (hasId && !type.IsValueType) {
				byId[id] = instance;
			}

			foreach (var field in SerializationFields.Of(type)) {
				if (node.TryGet(field.Name, out var fieldNode)) {
					field.SetValue(instance, Read(fieldNode, field.FieldType));
				}
			}
			return instance;
		}

		/// <summary>
		/// Allocates an instance without running a constructor, which is what OdinSerializer does and
		/// therefore what every existing save assumes. It is also the only option available: none of v1's
		/// save types declares a parameterless constructor, a fact pinned by a test in
		/// <c>SaveLoadRoundTripTests</c>. Any field the save does not mention keeps its zero value.
		/// </summary>
		private static object Allocate(Type type) {
			return type.IsValueType ? Activator.CreateInstance(type) : System.Runtime.Serialization.FormatterServices.GetUninitializedObject(type);
		}

		/// <summary>
		/// Constructs a collection properly — unlike a plain data object, a <see cref="List{T}"/> or
		/// <see cref="Dictionary{TKey, TValue}"/> allocated without its constructor has null internals and
		/// throws on the first Add.
		/// </summary>
		private static object Construct(Type type) {
			try {
				return Activator.CreateInstance(type, nonPublic: true);
			}
			catch (MissingMethodException exception) {
				throw new SerializationException($"'{type}' is a collection with no parameterless constructor, so it cannot be rebuilt from a save.", exception);
			}
		}

		private static int ParseId(JsonValue node) => int.Parse(node.RawNumber, NumberStyles.Integer, CultureInfo.InvariantCulture);
	}
}
