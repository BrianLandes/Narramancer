using System.Globalization;
using System.Text;

namespace Narramancer.Serialization.Json {

	/// <summary>Emits a <see cref="JsonValue"/> tree as compact JSON text.</summary>
	public static class JsonWriter {

		public static string Write(JsonValue value) {
			var builder = new StringBuilder();
			WriteValue(builder, value);
			return builder.ToString();
		}

		private static void WriteValue(StringBuilder builder, JsonValue value) {
			switch (value.Kind) {
				case JsonKind.Null:
					builder.Append("null");
					break;
				case JsonKind.Bool:
					builder.Append(value.AsBool ? "true" : "false");
					break;
				case JsonKind.Number:
					builder.Append(value.RawNumber);
					break;
				case JsonKind.String:
					WriteString(builder, value.AsString);
					break;
				case JsonKind.Array:
					builder.Append('[');
					for (var i = 0; i < value.Items.Count; i++) {
						if (i > 0) {
							builder.Append(',');
						}
						WriteValue(builder, value.Items[i]);
					}
					builder.Append(']');
					break;
				case JsonKind.Object:
					builder.Append('{');
					for (var i = 0; i < value.Members.Count; i++) {
						if (i > 0) {
							builder.Append(',');
						}
						WriteString(builder, value.Members[i].Key);
						builder.Append(':');
						WriteValue(builder, value.Members[i].Value);
					}
					builder.Append('}');
					break;
			}
		}

		private static void WriteString(StringBuilder builder, string value) {
			builder.Append('"');
			foreach (var character in value) {
				switch (character) {
					case '"': builder.Append("\\\""); break;
					case '\\': builder.Append("\\\\"); break;
					case '\b': builder.Append("\\b"); break;
					case '\f': builder.Append("\\f"); break;
					case '\n': builder.Append("\\n"); break;
					case '\r': builder.Append("\\r"); break;
					case '\t': builder.Append("\\t"); break;
					default:
						if (character < 0x20) {
							builder.Append("\\u");
							builder.Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
						}
						else {
							builder.Append(character);
						}
						break;
				}
			}
			builder.Append('"');
		}
	}
}
