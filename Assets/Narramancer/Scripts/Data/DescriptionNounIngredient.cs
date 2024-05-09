using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Narramancer
{
    public class DescriptionNounIngredient : AbstractNounIngredient
    {
        [SerializeField]
        [TextArea]
        string description = "";
        public string Description => description;
    }

    public static class DescriptionNounIngredientExtensions {

        public static string GetDescription(this NounInstance instance) {
            var ingredient = instance.GetIngredient<DescriptionNounIngredient>();
            if (ingredient != null) {
                return ingredient.Description;
			}
            return string.Empty;

        }
	}
}
