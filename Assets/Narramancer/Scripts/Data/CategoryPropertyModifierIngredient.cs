using System.Linq;
using UnityEngine;

namespace Narramancer {
	/// <summary>
	/// When added, removes any other properties from the same category.
	/// </summary>
	public class CategoryPropertyModifierIngredient : AbstractPropertyModifierIngredient {

		[SerializeField]
		string category = "Group";
		public string Category => category;

	}
}
