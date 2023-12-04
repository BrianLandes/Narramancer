using System;
using System.Collections.Generic;
using System.Linq;

namespace Narramancer {

	[Serializable]
	public class PropertyInstance : AdjectiveInstance<PropertyScriptableObject> {

		public PropertyInstance(PropertyScriptableObject adjective, NounInstance nounInstance) : base(adjective) {
			foreach (var modifier in adjective.Modifiers) {
				modifier.Initialize(this, nounInstance);
			}
		}

		public bool HasModifier<T>() where T : AbstractPropertyModifierIngredient {
			return Adjective.HasModifier<T>();
		}

		public bool HasModifier(Type type) {
			return Adjective.HasModifier(type);
		}

		public T GetModifier<T>() where T : AbstractPropertyModifierIngredient {
			return Adjective.GetModifier<T>();
		}

		public AbstractPropertyModifierIngredient GetModifier(Type type) {
			return Adjective.GetModifier(type);
		}

		public IEnumerable<T> GetModifiers<T>() where T : AbstractPropertyModifierIngredient {
			return Adjective.Modifiers.OfType<T>();
		}

	}
}