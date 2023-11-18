
using System;
using System.Collections.Generic;
using System.Linq;

namespace Narramancer {

	public static class ListExtensions {

		public static void Enqueue<T>(this List<T> list, T element) {
			list.Add(element);
		}

		public static T Dequeue<T>(this List<T> list) {
			if (list.Count == 0) {
				throw new SystemException("List Extensions: List was empty");
			}
			var element = list[0];
			list.RemoveAt(0);
			return element;
		}

		public static void Prepend<T>(this List<T> list, T element) {
			list.Insert(0, element);
		}

		public static T First<T>(this List<T> list) {
			if (list.Count == 0) {
				throw new SystemException("List Extensions: List was empty");
			}
			var element = list[0];
			return element;
		}

		public static T Last<T>(this List<T> list) {
			if (list.Count == 0) {
				throw new SystemException("List Extensions: List was empty");
			}
			var element = list[list.Count - 1];
			return element;
		}

		public static bool IsEmpty<T>(this List<T> list) {
			return list.Count == 0;
		}

		public static bool IsNotEmpty<T>(this List<T> list) {
			return list.Count > 0;
		}

		public static IEnumerable<T> WithoutNulls<T>(this IEnumerable<T> list) where T : class {
			return list.Where(item => item != null);
		}


		public static void Push<T>(this List<T> list, T element) => Prepend(list, element);

		public static T Peek<T>(this List<T> list) => First(list);

		public static T Pop<T>(this List<T> list) => Dequeue(list);


		public static bool ContainsAny<T>(this IEnumerable<T> list, IEnumerable<T> elements) {
			return list.Where(element => !elements.Contains(element)).Any();
		}

		public static bool ContainsAll<T>(this IEnumerable<T> list, IEnumerable<T> elements) {
			foreach (var element in elements) {
				if (!list.Contains(element)) {
					return false;
				}
			}
			return true;
		}

		public static void RemoveElements<T>(this List<T> list, IEnumerable<T> elements) {
			foreach (var element in elements) {
				if (list.Contains(element)) {
					list.Remove(element);
				}
			}
		}

		public static IEnumerable<T> Excluding<T>(this IEnumerable<T> list, T element) {
			return list.Where(x => !x.Equals(element));
		}

		public static IEnumerable<T> Excluding<T>(this IEnumerable<T> list, IEnumerable<T> elements) {
			return list.Where(x => !elements.Contains(x));
		}

		public static T ChooseOne<T>(this IEnumerable<T> enumerable) {
			if (enumerable.Count() == 0) {
				return default(T);
			}
			int roll = UnityEngine.Random.Range(0, enumerable.Count());
			T returnItem = enumerable.ElementAt(roll);

			return returnItem;
		}
	}
}