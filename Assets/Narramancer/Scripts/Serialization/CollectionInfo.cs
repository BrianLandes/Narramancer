using System;
using System.Collections.Generic;

namespace Narramancer.Serialization {

	/// <summary>
	/// Recognizes the collection shapes the serializer handles natively: arrays, <see cref="List{T}"/>
	/// and <see cref="Dictionary{TKey, TValue}"/>.
	/// <para>
	/// Both tests walk the base chain rather than matching the type exactly, because v1's save graph is
	/// full of <c>SerializableDictionary&lt;K, V&gt;</c> subclasses (<c>StringNodeRunnerDictionary</c>,
	/// <c>FlagIntDictionary</c>, …) which derive from <see cref="Dictionary{TKey, TValue}"/> several
	/// levels down. An exact match would send them through field reflection instead, serializing
	/// <c>SerializableDictionaryBase</c>'s <c>keys</c>/<c>values</c> backing arrays. Those arrays are
	/// Unity's <c>ISerializationCallbackReceiver</c> mechanism and are irrelevant here — populating the
	/// dictionary directly is both correct and roughly half the size.
	/// </para>
	/// </summary>
	internal static class CollectionInfo {

		public static bool TryList(Type type, out Type elementType) {
			for (var current = type; current != null && current != typeof(object); current = current.BaseType) {
				if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(List<>)) {
					elementType = current.GetGenericArguments()[0];
					return true;
				}
			}
			elementType = null;
			return false;
		}

		public static bool TryDictionary(Type type, out Type keyType, out Type valueType) {
			for (var current = type; current != null && current != typeof(object); current = current.BaseType) {
				if (current.IsGenericType && current.GetGenericTypeDefinition() == typeof(Dictionary<,>)) {
					var arguments = current.GetGenericArguments();
					keyType = arguments[0];
					valueType = arguments[1];
					return true;
				}
			}
			keyType = null;
			valueType = null;
			return false;
		}
	}
}
