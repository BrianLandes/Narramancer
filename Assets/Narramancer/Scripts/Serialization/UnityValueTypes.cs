using System;
using Narramancer.Serialization.Json;
using UnityEngine;

namespace Narramancer.Serialization {

	/// <summary>
	/// Unity value types whose state is unreachable by the normal field policy, written through their public
	/// properties instead.
	/// <para>
	/// <see cref="Rect"/> looks like it should reflect fine, and does not: its four fields are
	/// <c>private m_XMin, m_YMin, m_Width, m_Height</c> with no <c>[SerializeField]</c>, so "public or
	/// <c>[SerializeField]</c>" selects none of them. It serialized as <c>{}</c> and read back as
	/// <c>(0, 0, 0, 0)</c> — no error, because an empty object is perfectly valid JSON. That is how a saved
	/// sprite rect became a zero rect and the UI it drove came back blank.
	/// </para>
	/// <para>
	/// <see cref="Vector3"/>, <see cref="Color"/> and <see cref="Quaternion"/> are fine only because their
	/// fields happen to be public. The distinction is invisible from the outside, which is why anything not
	/// handled here is <b>refused</b> rather than guessed at — see <c>ObjectWriter</c>.
	/// </para>
	/// </summary>
	internal static class UnityValueTypes {

		public static bool Handles(Type type) =>
			type == typeof(Rect) || type == typeof(RectInt)
			|| type == typeof(Bounds) || type == typeof(BoundsInt)
			|| type == typeof(Vector2Int) || type == typeof(Vector3Int)
			|| type == typeof(LayerMask);

		public static JsonValue Write(object value, Type type) {
			var node = JsonValue.NewObject();

			if (type == typeof(Rect)) {
				var rect = (Rect)value;
				Add(node, "x", rect.x); Add(node, "y", rect.y);
				Add(node, "width", rect.width); Add(node, "height", rect.height);
				return node;
			}
			if (type == typeof(RectInt)) {
				var rect = (RectInt)value;
				Add(node, "x", rect.x); Add(node, "y", rect.y);
				Add(node, "width", rect.width); Add(node, "height", rect.height);
				return node;
			}
			if (type == typeof(Bounds)) {
				var bounds = (Bounds)value;
				node.Add("center", Vector(bounds.center));
				node.Add("size", Vector(bounds.size));
				return node;
			}
			if (type == typeof(BoundsInt)) {
				var bounds = (BoundsInt)value;
				Add(node, "x", bounds.position.x); Add(node, "y", bounds.position.y); Add(node, "z", bounds.position.z);
				Add(node, "sx", bounds.size.x); Add(node, "sy", bounds.size.y); Add(node, "sz", bounds.size.z);
				return node;
			}
			if (type == typeof(Vector2Int)) {
				var vector = (Vector2Int)value;
				Add(node, "x", vector.x); Add(node, "y", vector.y);
				return node;
			}
			if (type == typeof(Vector3Int)) {
				var vector = (Vector3Int)value;
				Add(node, "x", vector.x); Add(node, "y", vector.y); Add(node, "z", vector.z);
				return node;
			}
			if (type == typeof(LayerMask)) {
				Add(node, "value", ((LayerMask)value).value);
				return node;
			}

			throw new SerializationException($"'{type}' is not a handled Unity value type.");
		}

		public static object Read(JsonValue node, Type type) {
			if (type == typeof(Rect)) {
				return new Rect(Float(node, "x"), Float(node, "y"), Float(node, "width"), Float(node, "height"));
			}
			if (type == typeof(RectInt)) {
				return new RectInt(Int(node, "x"), Int(node, "y"), Int(node, "width"), Int(node, "height"));
			}
			if (type == typeof(Bounds)) {
				node.TryGet("center", out var center);
				node.TryGet("size", out var size);
				return new Bounds(ReadVector(center), ReadVector(size));
			}
			if (type == typeof(BoundsInt)) {
				return new BoundsInt(
					Int(node, "x"), Int(node, "y"), Int(node, "z"),
					Int(node, "sx"), Int(node, "sy"), Int(node, "sz"));
			}
			if (type == typeof(Vector2Int)) {
				return new Vector2Int(Int(node, "x"), Int(node, "y"));
			}
			if (type == typeof(Vector3Int)) {
				return new Vector3Int(Int(node, "x"), Int(node, "y"), Int(node, "z"));
			}
			if (type == typeof(LayerMask)) {
				return (LayerMask)Int(node, "value");
			}

			throw new SerializationException($"'{type}' is not a handled Unity value type.");
		}

		private static void Add(JsonValue node, string key, float value) =>
			node.Add(key, JsonValue.Number(value.ToString("R", System.Globalization.CultureInfo.InvariantCulture)));

		private static void Add(JsonValue node, string key, int value) =>
			node.Add(key, JsonValue.Number(value.ToString(System.Globalization.CultureInfo.InvariantCulture)));

		private static JsonValue Vector(Vector3 vector) {
			var node = JsonValue.NewObject();
			Add(node, "x", vector.x); Add(node, "y", vector.y); Add(node, "z", vector.z);
			return node;
		}

		private static Vector3 ReadVector(JsonValue node) =>
			node == null ? Vector3.zero : new Vector3(Float(node, "x"), Float(node, "y"), Float(node, "z"));

		private static float Float(JsonValue node, string key) =>
			node.TryGet(key, out var value)
				? float.Parse(value.RawNumber, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture)
				: 0f;

		private static int Int(JsonValue node, string key) =>
			node.TryGet(key, out var value)
				? int.Parse(value.RawNumber, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture)
				: 0;
	}
}
