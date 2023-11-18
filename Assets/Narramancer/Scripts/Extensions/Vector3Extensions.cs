
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Narramancer {
	public static class Vector3Extensions {

		public static bool ApproxEquals(this Vector3 self, Vector3 other, float threshold) {
			float diff = (self - other).sqrMagnitude;
			return diff < threshold * threshold;
		}

		public static bool Approximately(this Vector3 self, Vector3 other) {
			if (!Mathf.Approximately(self.x, other.x)) {
				return false;
			}
			if (!Mathf.Approximately(self.y, other.y)) {
				return false;
			}
			if (!Mathf.Approximately(self.z, other.z)) {
				return false;
			}
			return true;
		}

		public static Vector2 JustXZ(this Vector3 point) {
			return new Vector2(point.x, point.z);
		}

		public static Vector3 JustY(this Vector3 point) {
			return new Vector3(0, point.y, 0);
		}

		public static Vector3 DropY(this Vector3 point) {
			return new Vector3(point.x, 0, point.z);
		}

		public static Vector3 FromXZ(this Vector2 point) {
			return new Vector3(point.x, 0, point.y);
		}

		public static Vector3 ElementMultipy(this Vector3 self, Vector3 other) {
			return new Vector3(self.x * other.x, self.y * other.y, self.z * other.z);
		}

		public static Vector3 DivideBy(this Vector3 self, Vector3 other) {
			return new Vector3(self.x / other.x, self.y / other.y, self.z / other.z);
		}

		public static Vector3 Average(this IEnumerable<Vector3> elements) {
			var result = Vector3.zero;
			foreach (var element in elements) {
				result += element;
			}
			result /= Mathf.Max(1, elements.Count());
			return result;
		}

		public static Vector2 Average(this IEnumerable<Vector2> elements) {
			var result = Vector2.zero;
			foreach (var element in elements) {
				result += element;
			}
			result /= Mathf.Max(1, elements.Count());
			return result;
		}


		public static bool IsWithinDistanceOf(this Vector3 A, Vector3 B, float distance) {
			float x = A.x - B.x;
			if (Mathf.Abs(x) > distance) {
				return false;
			}
			float y = A.y - B.y;
			if (Mathf.Abs(y) > distance) {
				return false;
			}
			float z = A.z - B.z;
			if (Mathf.Abs(z) > distance) {
				return false;
			}

			// one 'cheat' we can do is check for zeroes
			if (distance > 0 && x == 0 && y == 0 && z == 0) {
				// who knows how much this hurts or helps?
				return true;
			}

			float distanceSqrd = distance * distance;
			return distanceSqrd > x * x + y * y + z * z;
		}



		public static bool IsWithinDistanceOf(this Vector2 A, Vector2 B, float distance) {
			float x = A.x - B.x;
			if (Mathf.Abs(x) > distance) {
				return false;
			}
			float y = A.y - B.y;
			if (Mathf.Abs(y) > distance) {
				return false;
			}

			// one 'cheat' we can do is check for zeroes
			if (distance > 0 && x == 0 && y == 0) {
				return true;
			}

			float distanceSqrd = distance * distance;
			return distanceSqrd > x * x + y * y;
		}

	}
}