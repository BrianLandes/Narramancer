using System;
using UnityEngine;

namespace Narramancer {

	[Serializable]
	public class VectorFieldTable {

		public float minX = 0f;

		public float maxX = 1f;

		public float minY = 0f;

		public float maxY = 1f;

		public float resultForMinXMinY = 0;

		public float resultForMaxXMinY = 0.5f;

		public float resultForMinXMaxY = 0.5f;

		public float resultForMaxXMaxY = 1;

		public float Calculate(float x, float y) {
			return VectorFieldTableUtilities.CalculateValue(x, y,
				minX, maxX, minY, maxY,
				resultForMinXMinY, resultForMaxXMinY, resultForMinXMaxY, resultForMaxXMaxY);
		}
	}

	public static class VectorFieldTableUtilities {
		public static float CalculateValue(float x, float y,
				float minX, float maxX, float minY, float maxY,
				float resultForMinXMinY, float resultForMaxXMinY, float resultForMinXMaxY, float resultForMaxXMaxY) {

			var normalizedX = Normalize(x, minX, maxX);
			var normalizedY = Normalize(y, minY, maxY);

			float minHorizontalLerp = Mathf.Lerp(resultForMinXMinY, resultForMaxXMinY, normalizedX);
			float maxHorizontalLerp = Mathf.Lerp(resultForMinXMaxY, resultForMaxXMaxY, normalizedX);

			return Mathf.Lerp(minHorizontalLerp, maxHorizontalLerp, normalizedY);
		}

		public static float Normalize(float value, float min, float max) {
			float clampedValue = Mathf.Clamp(value, min, max);
			float normalizedValue = (clampedValue - min) / (max - min);
			return normalizedValue;
		}
	}
}