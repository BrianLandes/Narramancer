using System;
using System.Globalization;
using System.Text;

namespace Narramancer.Serialization.Json {

	/// <summary>
	/// A small recursive-descent JSON parser. Hand-written so the serializer takes no dependency on
	/// a JSON library — the whole point of the exercise is owning this code outright.
	/// </summary>
	public static class JsonParser {

		public static JsonValue Parse(string text) {
			if (text == null) {
				throw new ArgumentNullException(nameof(text));
			}

			var index = 0;
			SkipWhitespace(text, ref index);
			var value = ParseValue(text, ref index);
			SkipWhitespace(text, ref index);
			if (index != text.Length) {
				throw Error(index, "unexpected trailing characters");
			}
			return value;
		}

		private static JsonValue ParseValue(string s, ref int i) {
			if (i >= s.Length) {
				throw Error(i, "unexpected end of input");
			}

			switch (s[i]) {
				case '{': return ParseObject(s, ref i);
				case '[': return ParseArray(s, ref i);
				case '"': return JsonValue.String(ParseString(s, ref i));
				case 't':
				case 'f': return ParseBool(s, ref i);
				case 'n': Expect(s, ref i, "null"); return JsonValue.Null;
				default: return ParseNumber(s, ref i);
			}
		}

		private static JsonValue ParseObject(string s, ref int i) {
			i++; // consume '{'
			var obj = JsonValue.NewObject();
			SkipWhitespace(s, ref i);
			if (Peek(s, i) == '}') {
				i++;
				return obj;
			}

			while (true) {
				SkipWhitespace(s, ref i);
				if (Peek(s, i) != '"') {
					throw Error(i, "expected a property name");
				}

				var key = ParseString(s, ref i);
				SkipWhitespace(s, ref i);
				if (Peek(s, i) != ':') {
					throw Error(i, "expected ':'");
				}

				i++;
				SkipWhitespace(s, ref i);
				obj.Add(key, ParseValue(s, ref i));
				SkipWhitespace(s, ref i);

				var next = Peek(s, i);
				if (next == ',') {
					i++;
					continue;
				}
				if (next == '}') {
					i++;
					break;
				}
				throw Error(i, "expected ',' or '}'");
			}

			return obj;
		}

		private static JsonValue ParseArray(string s, ref int i) {
			i++; // consume '['
			var array = JsonValue.NewArray();
			SkipWhitespace(s, ref i);
			if (Peek(s, i) == ']') {
				i++;
				return array;
			}

			while (true) {
				SkipWhitespace(s, ref i);
				array.Add(ParseValue(s, ref i));
				SkipWhitespace(s, ref i);

				var next = Peek(s, i);
				if (next == ',') {
					i++;
					continue;
				}
				if (next == ']') {
					i++;
					break;
				}
				throw Error(i, "expected ',' or ']'");
			}

			return array;
		}

		private static string ParseString(string s, ref int i) {
			i++; // consume opening quote
			var builder = new StringBuilder();
			while (true) {
				if (i >= s.Length) {
					throw Error(i, "unterminated string");
				}

				var character = s[i++];
				if (character == '"') {
					break;
				}
				if (character != '\\') {
					builder.Append(character);
					continue;
				}
				if (i >= s.Length) {
					throw Error(i, "unterminated escape sequence");
				}

				var escape = s[i++];
				switch (escape) {
					case '"': builder.Append('"'); break;
					case '\\': builder.Append('\\'); break;
					case '/': builder.Append('/'); break;
					case 'b': builder.Append('\b'); break;
					case 'f': builder.Append('\f'); break;
					case 'n': builder.Append('\n'); break;
					case 'r': builder.Append('\r'); break;
					case 't': builder.Append('\t'); break;
					case 'u':
						if (i + 4 > s.Length) {
							throw Error(i, "invalid \\u escape");
						}
						builder.Append((char)ushort.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
						i += 4;
						break;
					default:
						throw Error(i, "invalid escape character");
				}
			}

			return builder.ToString();
		}

		private static JsonValue ParseBool(string s, ref int i) {
			if (Peek(s, i) == 't') {
				Expect(s, ref i, "true");
				return JsonValue.Bool(true);
			}
			Expect(s, ref i, "false");
			return JsonValue.Bool(false);
		}

		private static JsonValue ParseNumber(string s, ref int i) {
			var start = i;
			while (i < s.Length) {
				var character = s[i];
				if (char.IsDigit(character) || character == '-' || character == '+' || character == '.' || character == 'e' || character == 'E') {
					i++;
				}
				else {
					break;
				}
			}

			if (i == start) {
				throw Error(i, "invalid value");
			}

			return JsonValue.Number(s.Substring(start, i - start));
		}

		private static void Expect(string s, ref int i, string literal) {
			if (i + literal.Length > s.Length || s.Substring(i, literal.Length) != literal) {
				throw Error(i, $"expected '{literal}'");
			}
			i += literal.Length;
		}

		private static char Peek(string s, int i) => i < s.Length ? s[i] : '\0';

		private static void SkipWhitespace(string s, ref int i) {
			while (i < s.Length) {
				var character = s[i];
				if (character == ' ' || character == '\t' || character == '\n' || character == '\r') {
					i++;
				}
				else {
					break;
				}
			}
		}

		private static SerializationException Error(int index, string message) =>
			new SerializationException($"JSON parse error at index {index}: {message}.");
	}
}
