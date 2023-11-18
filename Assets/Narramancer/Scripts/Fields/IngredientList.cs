
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Narramancer {
	[Serializable]
	public class IngredientList<T> : IEnumerable<T> where T : AbstractIngredient {

		#region Values Member

		[SerializeField]
		private List<T> values = new List<T>();

		#endregion

		public IngredientList() {
		}

		#region Add, Remove, Indexing Methods

		public T this[int i] {
			get {
				return values[i];
			}
			set {
				if (i < 0) {
					throw new ArgumentException();
				}
				else
				if (i >= values.Count) {
					values.Add(value);
				}
				else {
					values[i] = value;
				}
			}
		}

		public void Add(T item, bool allowDuplicates = false) {
			if (!values.Contains(item) || allowDuplicates) {
				values.Add(item);
			}
		}

		public void AddRange(IEnumerable<T> list, bool allowDuplicates = false) {
			foreach (var item in list) {
				Add(item, allowDuplicates);
			}
		}

		public void Remove(T item) {
			if (values.Contains(item)) {
				values.Remove(item);
			}
		}
		public bool Contains(T value) {
			return values.Contains(value);
		}

		public void Clear() {
			values.Clear();
		}

		#endregion

		#region IEnumerable Methods

		public IEnumerator<T> GetEnumerator() {
			return values.WithoutNulls().GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		#endregion

		#region Get Ingredient

		public bool HasIngredient<Y>() where Y : T {
			foreach (var ingredient in this) {
				if (typeof(Y).IsAssignableFrom(ingredient.GetType())) {
					return true;
				}
			}
			return false;
		}

		public bool HasIngredient(Type ingredientType) {
			foreach (var ingredient in this) {
				if (ingredientType.IsAssignableFrom(ingredient.GetType())) {
					return true;
				}
			}
			return false;
		}

		public bool TryGetIngredient<Y>(out Y resultIngredient) where Y : T {
			foreach (var ingredient in this) {
				if (typeof(Y).IsAssignableFrom(ingredient.GetType())) {
					resultIngredient = (Y)ingredient;
					return true;
				}
			}
			resultIngredient = null;
			return false;
		}

		public bool TryGetIngredient(Type ingredientType, out T resultIngredient) {
			foreach (var ingredient in this) {
				if (ingredientType.IsAssignableFrom(ingredient.GetType())) {
					resultIngredient = ingredient;
					return true;
				}
			}
			resultIngredient = null;
			return false;
		}

		#endregion

	}
}