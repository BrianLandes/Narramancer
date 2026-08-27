using System;
using System.Globalization;
using Narramancer.Serialization.Json;

namespace Narramancer.Serialization {

	/// <summary>
	/// Narramancer's save serializer — the replacement for OdinSerializer.
	/// <para>
	/// It handles the object graph reachable from a <c>StoryInstance</c>: reflected fields over the whole
	/// inheritance chain, arrays / <c>List&lt;T&gt;</c> / <c>Dictionary&lt;K,V&gt;</c> and their
	/// subclasses, polymorphism through a <c>$type</c> tag, shared and cyclic references through
	/// <c>$id</c>/<c>$ref</c>, and references to authored Unity assets through an
	/// <see cref="IReferenceResolver"/> id handle.
	/// </para>
	/// <para>
	/// Why replace Odin at all: it ships as its own assembly, and a buyer who already owns Odin Inspector
	/// gets a duplicate-assembly conflict on import. The serializer being ours also makes the two
	/// save-breaking bugs in docs/ODIN_REMOVAL_PLAN.md fixable rather than vendored.
	/// </para>
	/// </summary>
	public sealed class NarramancerSerializer {

		/// <summary>Names the concrete type of a value whose slot is declared as something more general.</summary>
		public const string TypeKey = "$type";

		/// <summary>Carries a scalar that needed a <see cref="TypeKey"/> alongside it.</summary>
		public const string ValueKey = "$value";

		/// <summary>The identity assigned to a reference object the first time it is written.</summary>
		public const string IdKey = "$id";

		/// <summary>Points back at an already-written <see cref="IdKey"/> — the shared/cyclic case.</summary>
		public const string ReferenceKey = "$ref";

		/// <summary>A stable id for an asset the save points at but does not own.</summary>
		public const string AssetKey = "$asset";

		/// <summary>A <see cref="System.Type"/> value, stored by name.</summary>
		public const string SystemTypeKey = "$systemType";

		/// <summary>The elements of an array or list.</summary>
		public const string ValuesKey = "$values";

		/// <summary>The entries of a dictionary.</summary>
		public const string EntriesKey = "$entries";

		/// <summary>The key half of a dictionary entry.</summary>
		public const string EntryKeyKey = "k";

		/// <summary>The value half of a dictionary entry.</summary>
		public const string EntryValueKey = "v";

		public const string SchemaVersionKey = "schemaVersion";
		public const string PayloadKey = "payload";

		/// <summary>
		/// The version stamped into every envelope this serializer writes. Bump it whenever a change would
		/// make an older save read incorrectly — a save that announces its version can be rejected with a
		/// clear message or migrated, where an unversioned one can only be misread.
		/// </summary>
		public const int CurrentSchemaVersion = 1;

		private readonly IReferenceResolver references;

		/// <summary>
		/// A serializer with no reference resolver. Only valid for graphs that hold no Unity objects —
		/// anything else throws on write rather than inlining an asset.
		/// </summary>
		public NarramancerSerializer() : this(null) {
		}

		public NarramancerSerializer(IReferenceResolver references) {
			this.references = references;
		}

		/// <summary>The seam that turns Unity asset references into stable ids, or null if there is none.</summary>
		public IReferenceResolver References => references;

		public string Serialize<T>(T value) {
			return JsonWriter.Write(new ObjectWriter(references).Write(value, typeof(T)));
		}

		public T Deserialize<T>(string json) {
			return (T)new ObjectReader(references).Read(JsonParser.Parse(json), typeof(T));
		}

		/// <summary>
		/// Serializes a payload wrapped as <c>{ schemaVersion, payload }</c>, so a future load can detect
		/// an incompatible save instead of crashing partway through one.
		/// </summary>
		public string SerializeSave<T>(T value) {
			var root = JsonValue.NewObject();
			root.Add(SchemaVersionKey, JsonValue.Number(CurrentSchemaVersion.ToString(CultureInfo.InvariantCulture)));
			root.Add(PayloadKey, new ObjectWriter(references).Write(value, typeof(T)));
			return JsonWriter.Write(root);
		}

		/// <inheritdoc cref="SerializeSave{T}"/>
		public T DeserializeSave<T>(string json, out int schemaVersion) {
			var root = JsonParser.Parse(json);
			if (root.Kind != JsonKind.Object
				|| !root.TryGet(SchemaVersionKey, out var versionNode)
				|| !root.TryGet(PayloadKey, out var payloadNode)) {
				throw new SerializationException($"Expected a save envelope holding '{SchemaVersionKey}' and '{PayloadKey}'.");
			}

			schemaVersion = int.Parse(versionNode.RawNumber, NumberStyles.Integer, CultureInfo.InvariantCulture);
			if (schemaVersion > CurrentSchemaVersion) {
				throw new SerializationException($"This save was written by a newer version of the game (save format {schemaVersion}, this build reads {CurrentSchemaVersion}).");
			}

			return (T)new ObjectReader(references).Read(payloadNode, typeof(T));
		}
	}
}
