using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Narramancer {

	[Serializable]
	public class SerializablePrimitive {

		public string type;
		public bool boolValue;
		public int intValue;
		public float floatValue;
		public string stringValue;
		public Color color;
		public Vector2 vector2;
		public Vector3 vector3;
		public UnityEngine.Object objectValue;

		public object GetValue() {
			switch (type) {
				case "int":
					return intValue;
				case "bool":
					return boolValue;
				case "float":
					return floatValue;
				case "string":
					return stringValue;
				case nameof(color):
					return color;
				case nameof(vector2):
					return vector2;
				case nameof(vector3):
					return vector3;
				default:
					return objectValue;
			}
		}

		public void SetType(Type type) {
			this.type = TypeToString(type);
		}

		public static string TypeToString(Type type) {
			if (typeof(int) == type) {
				return "int";
			}
			if (typeof(float) == type) {
				return "float";
			}
			if (typeof(bool) == type) {
				return "bool";
			}
			if (typeof(string) == type) {
				return "string";
			}
			if (typeof(Color) == type) {
				return nameof(color);
			}
			if (typeof(Vector2) == type) {
				return nameof(vector2);
			}
			if (typeof(Vector3) == type) {
				return nameof(vector3);
			}
			return type.AssemblyQualifiedName;
		}

		public static bool IsSupportedType(Type type ) {
			if (typeof(int) == type) {
				return true;
			}
			if (typeof(float) == type) {
				return true;
			}
			if (typeof(bool) == type) {
				return true;
			}
			if (typeof(string) == type) {
				return true;
			}
			if (typeof(Color) == type) {
				return true;
			}
			if (typeof(Vector2) == type) {
				return true;
			}
			if (typeof(Vector3) == type) {
				return true;
			}
			if (typeof(UnityEngine.Object).IsAssignableFrom( type)) {
				return true;
			}
			return false;
		}
	}

}