using System.Collections.Generic;

namespace Narramancer.Serialization.Json {

	public enum JsonKind {
		Null,
		Bool,
		Number,
		String,
		Array,
		Object,
	}

	/// <summary>
	/// A minimal JSON document node. Numbers are kept in their textual form so the exact value
	/// (and, with a type tag, the intended CLR type) can be reconstructed without precision loss.
	/// Object members preserve insertion order, so serialized output is stable — which matters for
	/// diffing two saves and for the golden-file checks in the Odin removal plan.
	/// </summary>
	public sealed class JsonValue {

		private bool boolValue;
		private string rawNumber;
		private string stringValue;
		private List<JsonValue> items;
		private List<KeyValuePair<string, JsonValue>> members;

		private JsonValue(JsonKind kind) {
			Kind = kind;
		}

		public JsonKind Kind { get; }

		public static readonly JsonValue Null = new JsonValue(JsonKind.Null);

		public static JsonValue Bool(bool value) => new JsonValue(JsonKind.Bool) { boolValue = value };

		public static JsonValue Number(string raw) => new JsonValue(JsonKind.Number) { rawNumber = raw };

		public static JsonValue String(string value) => new JsonValue(JsonKind.String) { stringValue = value };

		public static JsonValue NewArray() => new JsonValue(JsonKind.Array) { items = new List<JsonValue>() };

		public static JsonValue NewObject() => new JsonValue(JsonKind.Object) { members = new List<KeyValuePair<string, JsonValue>>() };

		public bool AsBool => boolValue;

		public string RawNumber => rawNumber;

		public string AsString => stringValue;

		public IReadOnlyList<JsonValue> Items => items;

		public IReadOnlyList<KeyValuePair<string, JsonValue>> Members => members;

		public void Add(JsonValue item) => items.Add(item);

		public void Add(string key, JsonValue value) => members.Add(new KeyValuePair<string, JsonValue>(key, value));

		public bool TryGet(string key, out JsonValue value) {
			if (members != null) {
				for (var i = 0; i < members.Count; i++) {
					if (members[i].Key == key) {
						value = members[i].Value;
						return true;
					}
				}
			}
			value = null;
			return false;
		}
	}
}
