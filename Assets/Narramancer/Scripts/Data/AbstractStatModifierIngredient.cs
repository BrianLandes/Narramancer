namespace Narramancer {
	public abstract class AbstractStatModifierIngredient : AbstractIngredient {

		/// <summary>
		/// Takes in the given character and the current value of the stat.
		/// Determines and returns a 'term', that is then added to the current value. 
		/// </summary>
		public virtual float GetEffectiveValue(object context, NounInstance instance, float currentValue) {
			return 0f;
		}

	}
}