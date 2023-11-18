using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;


namespace Narramancer.SerializableDictionary {
	public abstract class SerializableDictionaryBase<TKey, TValue, TValueStorage> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver {
		[SerializeField]
		TKey[] keys;
		[SerializeField]
		TValueStorage[] values;

		public SerializableDictionaryBase() {
		}

		public SerializableDictionaryBase(IDictionary<TKey, TValue> dict) : base(dict.Count) {
			foreach (var kvp in dict) {
				this[kvp.Key] = kvp.Value;
			}
		}

		protected SerializableDictionaryBase(SerializationInfo info, StreamingContext context) : base(info, context) { }

		protected abstract void SetValue(TValueStorage[] storage, int i, TValue value);
		protected abstract TValue GetValue(TValueStorage[] storage, int i);

		public void CopyFrom(IDictionary<TKey, TValue> dict) {
			this.Clear();
			foreach (var kvp in dict) {
				this[kvp.Key] = kvp.Value;
			}
		}

		public void OnAfterDeserialize() {
			if (keys != null && values != null && keys.Length == values.Length) {
				this.Clear();
				int n = keys.Length;
				for (int i = 0; i < n; ++i) {
					this[keys[i]] = GetValue(values, i);
				}

				keys = null;
				values = null;
			}

		}

		public void OnBeforeSerialize() {
			int n = this.Count;
			keys = new TKey[n];
			values = new TValueStorage[n];

			int i = 0;
			foreach (var kvp in this) {
				keys[i] = kvp.Key;
				SetValue(values, i, kvp.Value);
				++i;
			}
		}
	}

	public class SerializableDictionary<TKey, TValue> : SerializableDictionaryBase<TKey, TValue, TValue> {
		public SerializableDictionary() {
		}

		public SerializableDictionary(IDictionary<TKey, TValue> dict) : base(dict) {
		}

		protected SerializableDictionary(SerializationInfo info, StreamingContext context) : base(info, context) { }

		protected override TValue GetValue(TValue[] storage, int i) {
			return storage[i];
		}

		protected override void SetValue(TValue[] storage, int i, TValue value) {
			storage[i] = value;
		}
	}

	public static class SerializableDictionary {
		public class Storage<T> {
			public T data;
		}
	}

	public class SerializableDictionary<TKey, TValue, TValueStorage> : SerializableDictionaryBase<TKey, TValue, TValueStorage> where TValueStorage : SerializableDictionary.Storage<TValue>, new() {
		public SerializableDictionary() {
		}

		public SerializableDictionary(IDictionary<TKey, TValue> dict) : base(dict) {
		}

		protected SerializableDictionary(SerializationInfo info, StreamingContext context) : base(info, context) { }

		protected override TValue GetValue(TValueStorage[] storage, int i) {
			return storage[i].data;
		}

		protected override void SetValue(TValueStorage[] storage, int i, TValue value) {
			storage[i] = new TValueStorage();
			storage[i].data = value;
		}
	}
}