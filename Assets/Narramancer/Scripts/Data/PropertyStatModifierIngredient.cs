
namespace Narramancer {
	public class PropertyStatModifierIngredient : AbstractStatModifierIngredient {

		public PropertyScriptableObject property;

		public float term = 1f;

		public override float GetEffectiveValue(object context, NounInstance instance, float currentValue) {

			if (instance.HasProperty(property)) {
				return term;
			}

			return 0;
		}
	}
}