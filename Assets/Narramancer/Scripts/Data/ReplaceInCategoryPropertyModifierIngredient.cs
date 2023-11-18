using System.Linq;
using UnityEngine;

namespace Narramancer {
	/// <summary>
	/// When added, removes any other properties from the same category.
	/// </summary>
	public class ReplaceInCategoryPropertyModifierIngredient : AbstractPropertyModifierIngredient {

		[SerializeField]
		string category = "Group";
		public string Category => category;

		public override void OnAdded(PropertyInstance propertyInstance, NounInstance nounInstance) {

			var sameCategory = nounInstance
				.GetPropertiesWith<ReplaceInCategoryPropertyModifierIngredient>()
				.Excluding(propertyInstance)
				.Where(property => property.GetModifier<ReplaceInCategoryPropertyModifierIngredient>().Category.Equals(category, System.StringComparison.OrdinalIgnoreCase))
				.ToArray();

			nounInstance.RemoveProperties(sameCategory);
		}
	}
}
