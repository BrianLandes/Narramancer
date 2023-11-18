using UnityEngine;
namespace Narramancer {
	public class StatAsFactorStatModifierIngredient : AbstractStatModifierIngredient {

		[SerializeField]
		private StatScriptableObject stat = default;

		[SerializeField]
		private float factor = 1;

		public override float GetEffectiveValue(object context, NounInstance instance, float currentValue) {
			var statValue = instance.GetStatEffectiveValue(context, stat);
			return statValue * factor;
		}
	}
}