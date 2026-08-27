using System;
using System.Globalization;
using Narramancer.Serialization.Json;

namespace Narramancer.Serialization {

	/// <summary>
	/// The leaf value types the serializer writes directly, with no identity or reference tracking:
	/// the numeric primitives, <see cref="bool"/>, <see cref="char"/>, <see cref="string"/>, enums,
	/// and the two value-like framework types that turn up in save data (<see cref="DateTime"/> and
	/// <see cref="Guid"/>). Numbers are written in a round-trippable textual form; the intended CLR
	/// type is recovered from the slot's declared type or from a <c>$type</c> tag.
	/// </summary>
	internal static class Scalars {

		public static bool IsScalar(Type type) {
			if (type.IsEnum) {
				return true;
			}
			return type == typeof(bool)
				|| type == typeof(byte)
				|| type == typeof(sbyte)
				|| type == typeof(short)
				|| type == typeof(ushort)
				|| type == typeof(int)
				|| type == typeof(uint)
				|| type == typeof(long)
				|| type == typeof(ulong)
				|| type == typeof(float)
				|| type == typeof(double)
				|| type == typeof(decimal)
				|| type == typeof(char)
				|| type == typeof(string)
				|| type == typeof(DateTime)
				|| type == typeof(Guid);
		}

		public static JsonValue Write(object value) {
			switch (value) {
				case Enum enumValue: return JsonValue.String(enumValue.ToString());
				case bool boolValue: return JsonValue.Bool(boolValue);
				case string stringValue: return JsonValue.String(stringValue);
				case char charValue: return JsonValue.String(charValue.ToString());
				case byte byteValue: return JsonValue.Number(byteValue.ToString(CultureInfo.InvariantCulture));
				case sbyte sbyteValue: return JsonValue.Number(sbyteValue.ToString(CultureInfo.InvariantCulture));
				case short shortValue: return JsonValue.Number(shortValue.ToString(CultureInfo.InvariantCulture));
				case ushort ushortValue: return JsonValue.Number(ushortValue.ToString(CultureInfo.InvariantCulture));
				case int intValue: return JsonValue.Number(intValue.ToString(CultureInfo.InvariantCulture));
				case uint uintValue: return JsonValue.Number(uintValue.ToString(CultureInfo.InvariantCulture));
				case long longValue: return JsonValue.Number(longValue.ToString(CultureInfo.InvariantCulture));
				case ulong ulongValue: return JsonValue.Number(ulongValue.ToString(CultureInfo.InvariantCulture));
				case decimal decimalValue: return JsonValue.Number(decimalValue.ToString(CultureInfo.InvariantCulture));

				// "R" round-trips exactly; a plain ToString() loses the low bits of a float.
				case float floatValue: return JsonValue.Number(FiniteOrThrow(floatValue.ToString("R", CultureInfo.InvariantCulture), value));
				case double doubleValue: return JsonValue.Number(FiniteOrThrow(doubleValue.ToString("R", CultureInfo.InvariantCulture), value));

				// "O" is the round-trippable form and preserves DateTimeKind.
				case DateTime dateTimeValue: return JsonValue.String(dateTimeValue.ToString("O", CultureInfo.InvariantCulture));
				case Guid guidValue: return JsonValue.String(guidValue.ToString("D", CultureInfo.InvariantCulture));

				default:
					throw new SerializationException($"'{value.GetType()}' is not a serializable scalar.");
			}
		}

		public static object Read(JsonValue node, Type type) {
			if (type.IsEnum) {
				var name = node.AsString;
				try {
					return Enum.Parse(type, name);
				}
				catch (ArgumentException exception) {
					throw new SerializationException($"'{name}' is not a member of the enum '{type}'. Was it renamed or removed since the save was written?", exception);
				}
			}

			if (type == typeof(bool)) return node.AsBool;
			if (type == typeof(string)) return node.AsString;
			if (type == typeof(char)) return node.AsString[0];
			if (type == typeof(byte)) return byte.Parse(node.RawNumber, NumberStyles.Integer, CultureInfo.InvariantCulture);
			if (type == typeof(sbyte)) return sbyte.Parse(node.RawNumber, NumberStyles.Integer, CultureInfo.InvariantCulture);
			if (type == typeof(short)) return short.Parse(node.RawNumber, NumberStyles.Integer, CultureInfo.InvariantCulture);
			if (type == typeof(ushort)) return ushort.Parse(node.RawNumber, NumberStyles.Integer, CultureInfo.InvariantCulture);
			if (type == typeof(int)) return int.Parse(node.RawNumber, NumberStyles.Integer, CultureInfo.InvariantCulture);
			if (type == typeof(uint)) return uint.Parse(node.RawNumber, NumberStyles.Integer, CultureInfo.InvariantCulture);
			if (type == typeof(long)) return long.Parse(node.RawNumber, NumberStyles.Integer, CultureInfo.InvariantCulture);
			if (type == typeof(ulong)) return ulong.Parse(node.RawNumber, NumberStyles.Integer, CultureInfo.InvariantCulture);
			if (type == typeof(float)) return float.Parse(node.RawNumber, NumberStyles.Float, CultureInfo.InvariantCulture);
			if (type == typeof(double)) return double.Parse(node.RawNumber, NumberStyles.Float, CultureInfo.InvariantCulture);
			if (type == typeof(decimal)) return decimal.Parse(node.RawNumber, NumberStyles.Float, CultureInfo.InvariantCulture);
			if (type == typeof(DateTime)) return DateTime.Parse(node.AsString, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
			if (type == typeof(Guid)) return Guid.Parse(node.AsString);

			throw new SerializationException($"'{type}' is not a serializable scalar type.");
		}

		/// <summary>
		/// NaN and the infinities have no JSON representation. Odin wrote them as bare tokens that are not
		/// legal JSON; rather than inherit that, refuse them at the point they enter the save, where the
		/// message can still name the value.
		/// </summary>
		private static string FiniteOrThrow(string text, object value) {
			if (text.IndexOf("NaN", StringComparison.Ordinal) >= 0 || text.IndexOf("Infinity", StringComparison.Ordinal) >= 0) {
				throw new SerializationException($"'{value}' cannot be written to JSON — NaN and infinity have no JSON representation.");
			}
			return text;
		}
	}
}
