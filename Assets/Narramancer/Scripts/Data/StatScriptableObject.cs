
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Narramancer {

	[CreateAssetMenu(menuName = "Narramancer/Stat", fileName = "New Stat")]
	public class StatScriptableObject : AdjectiveScriptableObject {

		[SerializeField]
		ToggleableFloat startingValue = new ToggleableFloat(false, 0f);
		public ToggleableFloat StartingValue => startingValue;

		[SerializeField]
		ToggleableFloat minValue = new ToggleableFloat(false, 0f);
		public ToggleableFloat MinValue => minValue;

		[SerializeField]
		ToggleableFloat maxValue = new ToggleableFloat(false, 1f);
		public ToggleableFloat MaxValue => maxValue;

		[SerializeField]
		protected IngredientList<AbstractStatModifierIngredient> modifiers = new IngredientList<AbstractStatModifierIngredient>();
		public List<AbstractStatModifierIngredient> Modifiers => modifiers.ToList();

	}
}