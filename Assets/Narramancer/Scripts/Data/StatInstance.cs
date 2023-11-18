
using System;
using UnityEngine;

namespace Narramancer {

	[Serializable]
	public class StatInstance : AdjectiveInstance<StatScriptableObject> {

		[SerializeField]
		private float value;
		public static string ValueFieldname => nameof(value);

		public float Value {
			get => value;
			set {
				this.value = value;
				if (Adjective.MinValue.activated) {
					this.value = Mathf.Max(value, Adjective.MinValue.value);
				}
				if (Adjective.MaxValue.activated) {
					this.value = Mathf.Min(value, Adjective.MaxValue.value);
				}
			}
		}

		public StatInstance(StatScriptableObject adjective, NounInstance instance) : base(adjective) {
			if ( adjective.StartingValue.activated)
			Value = adjective.StartingValue.value;
		}

		/// <summary>
		/// The effective value of a stat at any time is a linear combination of its base 'value' and any 'modifier's it may have
		/// </summary>
		public float GetEffectiveValue(NounInstance instance, object context) {

			float result = value;

			foreach (var modifier in Adjective.Modifiers) {
				float nextTerm = modifier.GetEffectiveValue(context, instance, result);
				result += nextTerm;
			}

			return result;
		}

		public float GetEffectiveValuePercentage(NounInstance instance, object context) {
			var effectiveValue = GetEffectiveValue(instance, context);
			// TODO: figure out how to treat stats without either a min or a max?
			var minValue = 0f;
			var maxValue = 1f;

			if (Adjective.MinValue.activated && Adjective.MaxValue.activated) {
				minValue = Adjective.MinValue.value;
				maxValue = Adjective.MaxValue.value;
			}

			if (minValue >= 0) {
				return (effectiveValue - minValue) / (maxValue - minValue);
			}

			if (effectiveValue >= 0) {
				return effectiveValue / maxValue;
			}

			// if min value is a negative value AND effective value is a negative value -> return a negative percentage based on the distance from effective value to min value
			return -1f * effectiveValue / minValue;
		}
	}
}