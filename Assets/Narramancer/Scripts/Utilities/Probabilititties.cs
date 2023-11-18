
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Narramancer {

	public static class Probabilititties {
		public static T ChooseOne<T>(IList<T> list, bool removeChosenFromList = false) {
			if (list.Count == 0) {
				throw new System.Exception("Cannot randomly choose from an empty list");
			}
			int roll = Random.Range(0, list.Count);
			T returnItem = list[roll];
			if (removeChosenFromList) {
				list.RemoveAt(roll);
			}
			return returnItem;
		}

		public static T ChooseOne<T>(IEnumerable<T> list) {
			return ChooseOne(list.ToList());
		}

		public static T ChooseOne<T>(IList<T> list, T defaultIfListIsEmpty, bool removeChosenFromList = false) {
			if (list.Count == 0) {
				return defaultIfListIsEmpty;
			}
			return ChooseOne(list, removeChosenFromList);
		}

		public static T ChooseOne<T>(IEnumerable<T> list, T defaultIfListIsEmpty) {
			if (list.Count() == 0) {
				return defaultIfListIsEmpty;
			}
			return ChooseOne(list);
		}

		public interface IWeightedElement {
			float GetWeight();
		}


		public static T ChooseOneWeighted<T>(IList<T> list, bool removeChosenFromList = false) where T : class, IWeightedElement {
			if (list.Count == 0) {
				throw new System.Exception("Cannot randomly choose from an empty list");
			}
			float totalRollWeight = 0;
			foreach (IWeightedElement element in list) {
				totalRollWeight += element.GetWeight();
			}

			float roll = Random.Range(0, totalRollWeight);
			float walk = 0;

			T selectedElement = null;
			foreach (T element in list) {
				walk += element.GetWeight();
				if (walk >= roll) {
					selectedElement = element;
					break;
				}
			}

			if (removeChosenFromList && selectedElement != null) {
				list.Remove(selectedElement);
			}
			return selectedElement;
		}

		public static T ChooseOneWeighted<T>(IEnumerable<T> list) where T : class, IWeightedElement {

			float totalRollWeight = 0;
			foreach (IWeightedElement element in list) {
				totalRollWeight += element.GetWeight();
			}

			float roll = Random.Range(0, totalRollWeight);
			float walk = 0;

			T selectedElement = null;
			foreach (T element in list) {
				walk += element.GetWeight();
				if (walk >= roll) {
					selectedElement = element;
					break;
				}
			}

			return selectedElement;
		}

		public static bool TryChooseOneWeighted<T>(IList<T> list, out T chosenElement, bool removeChosenFromList = false)
				where T : class, IWeightedElement {
			if (list.Count == 0) {
				chosenElement = null;
				return false;
			}
			chosenElement = ChooseOneWeighted(list, removeChosenFromList);
			return chosenElement != null;
		}


		public static T ChooseOneWeighted<T>(IDictionary<T, float> dictionary) {
			if (dictionary.Count == 0) {
				throw new System.Exception("Cannot randomly choose from an empty list");
			}
			float totalRollWeight = dictionary.Values.Sum();

			float roll = Random.Range(0, totalRollWeight);
			float walk = 0;

			T selectedElement = default;
			foreach (var pair in dictionary) {
				walk += pair.Value;
				if (walk >= roll) {
					selectedElement = pair.Key;
					break;
				}
			}

			return selectedElement;
		}


		public static bool RollCheck(float chance) {
			float roll = Random.Range(0f, 1f);
			if (roll <= 0) {
				return false;
			}
			return roll <= chance;
		}

		public static float PlusOrMinus(float maxValue) {
			return (Random.value * 2f - 1f) * maxValue;
		}

		/// <summary>
		/// Returns a random value in the range [-3,3] with gaussian distribution around 0
		/// </summary>
		public static float Gaussian() {
			var value = 0f;
			value += PlusOrMinus(1f);
			value += PlusOrMinus(1f);
			value += PlusOrMinus(1f);
			return value;
		}

		private static List<float> filteredNumbersMemory = new List<float>();

		public static float RangeFiltered(float min, float max) {
			var difference = max - min;
			var value = RangeFiltered();
			var result = min + difference * value;
			return result;
		}

		public static float RangeFiltered() {
			float value = Random.Range(0, 1f);
			if (filteredNumbersMemory.Count == 0) {
				filteredNumbersMemory.Add(value);
				return value;
			}

			// Reroll if two consecutive numbers differ by less than threshold
			if (filteredNumbersMemory.Count >= 1) {
				var lastValue = filteredNumbersMemory.Last();
				var difference = Mathf.Abs(lastValue - value);
				if (difference < 0.02f) {
					// Reroll
					return RangeFiltered();
				}
			}

			// Reroll if three consecutive numbers differ by less than threshold
			if (filteredNumbersMemory.Count >= 2) {
				var lastValue = filteredNumbersMemory.Last();
				var difference = Mathf.Abs(lastValue - value);
				var secondLastValue = filteredNumbersMemory[filteredNumbersMemory.Count - 2];
				var secondDifference = Mathf.Abs(secondLastValue - value);
				if (difference < 0.1f && secondDifference < 0.1f) {
					// Reroll
					return RangeFiltered();
				}
			}

			// Reroll if too many numbers at the top or bottom of the range within the last 5 values
			if (filteredNumbersMemory.Count >= 4) {
				var lastNumbers = filteredNumbersMemory.GetRange(filteredNumbersMemory.Count - 4, 4);
				IEnumerable<System.Tuple<float, float>> NumberPairs(List<float> numbers) {
					for (int i = 0; i < numbers.Count - 1; i++) {
						yield return new System.Tuple<float, float>(numbers[i], numbers[i + 1]);
					}
				}
				var numberPairs = NumberPairs(lastNumbers);
				bool IsInSameRange(float a, float b) {
					return (a > 0.5f && b > 0.5f) || (a < 0.5f && b < 0.5f);
				}
				if (numberPairs.All(pair => IsInSameRange(pair.Item1, pair.Item2))) {
					// Return a value specifically in the other range
					var lastValue = filteredNumbersMemory.Last();
					if (IsInSameRange(lastValue, 0.4f)) {
						value = Random.Range(0.5f, 1f);
					}
					else {
						value = Random.Range(0.0f, 0.5f);
					}
				}
			}

			if (filteredNumbersMemory.Count > 5) {
				filteredNumbersMemory = filteredNumbersMemory.GetRange(filteredNumbersMemory.Count - 5, 5);
			}

			// Valid, filtered value
			filteredNumbersMemory.Add(value);
			return value;
		}
	}
}