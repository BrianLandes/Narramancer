
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Narramancer {

	[CreateAssetMenu(menuName = "Narramancer/Property", fileName = "New Property")]
	public class PropertyScriptableObject : AdjectiveScriptableObject {

		[SerializeField]
		protected IngredientList<AbstractPropertyModifierIngredient> modifiers = new IngredientList<AbstractPropertyModifierIngredient>();
		public List<AbstractPropertyModifierIngredient> Modifiers => modifiers.ToList();

		[SerializeField]
		protected NamedPrimitiveValueList staticNamedValues = new NamedPrimitiveValueList();

#if UNITY_EDITOR
		[SerializeField]
		private ReferenceList references = new ReferenceList();
#endif

		public bool HasModifier<T>() where T : AbstractPropertyModifierIngredient {
			return modifiers.Any(modifier => modifier is T);
		}

		public bool HasModifier(Type type) {
			return modifiers.Any(modifier => type.IsAssignableFrom(modifier.GetType()));
		}

		public T GetModifier<T>() where T : AbstractPropertyModifierIngredient {
			return modifiers.FirstOrDefault(modifier => modifier is T) as T;
		}

		public AbstractPropertyModifierIngredient GetModifier(Type type) {
			return modifiers.FirstOrDefault(modifier => modifier.GetType() == type);
		}

		public List<T> GetModifiers<T>() where T : AbstractPropertyModifierIngredient {
			return modifiers.Where(modifier => modifier is T).Cast<T>().ToList();
		}

		public object GetNamedValue(string name) {
			return staticNamedValues.list.FirstOrDefault(value => value.name.Equals(name))?.value.GetValue();
		}
	}
}