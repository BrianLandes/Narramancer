using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {
	[Serializable]
	public class Blackboard : INodeContext {


		[SerializeField]
		private StringIntDictionary ints = default;

		[SerializeField]
		private StringBoolDictionary bools = default;

		[SerializeField]
		private StringFloatDictionary floats = default;

		[SerializeField]
		private StringStringDictionary strings = default;

		[NonSerialized] // NOT SERIALIZED
		private StringGameObjectDictionary gameObjects = default;

		[NonSerialized] // NOT SERIALIZED
		private StringComponentDictionary components = default;

		[SerializeField]
		private StringUnityObjectDictionary unityObjects = default;

		[SerializeField]
		private StringObjectDictionary objects = default;

		public static string UniqueKey(object owner, string key) {
			return key + " " + owner.GetHashCode().ToString();
		}

		public void Clear() {
			ints?.Clear();
			bools?.Clear();
			floats?.Clear();
			strings?.Clear();
			gameObjects?.Clear();
			components?.Clear();
			unityObjects?.Clear();
			objects?.Clear();
		}

		public Blackboard Copy() {
			var copy = new Blackboard();
			foreach ((var key, var value) in GetAll()) {
				copy.Set(key, value);
			}
			return copy;
		}

		public IEnumerable<(string, object)> GetAll() {
			if (ints != null) {
				foreach (var pair in ints) {
					yield return (pair.Key, pair.Value);
				}
			}
			if (bools != null) {
				foreach (var pair in bools) {
					yield return (pair.Key, pair.Value);
				}
			}
			if (floats != null) {
				foreach (var pair in floats) {
					yield return (pair.Key, pair.Value);
				}
			}
			if (strings != null) {
				foreach (var pair in strings) {
					yield return (pair.Key, pair.Value);
				}
			}
			if (gameObjects != null) {
				foreach (var pair in gameObjects) {
					yield return (pair.Key, pair.Value);
				}
			}
			if (components != null) {
				foreach (var pair in components) {
					yield return (pair.Key, pair.Value);
				}
			}
			if (unityObjects != null) {
				foreach (var pair in unityObjects) {
					yield return (pair.Key, pair.Value);
				}
			}
			if (objects != null) {
				foreach (var pair in objects) {
					yield return (pair.Key, pair.Value);
				}
			}
		}

		#region Ints

		public void RemoveInt(string key) {
			if (ints != null) {
				ints.Remove(key);
			}
		}

		public void SetInt(string key, int value) {
			if (ints == null) {
				ints = new StringIntDictionary();
			}
			ints[key] = value;
		}

		public int GetInt(string key, int defaultValue = 0) {
			if (ints == null) {
				return defaultValue;
			}
			if (ints.TryGetValue(key, out var value)) {
				return value;
			}
			return defaultValue;
		}

		// For convenience
		public void DecrementInt(string key) {
			var value = GetInt(key);
			SetInt(key, value - 1);
		}


		// For convenience
		public void IncrementInt(string key) {
			var value = GetInt(key);
			SetInt(key, value + 1);
		}

		public bool TryGetInt(string key, out int intValue, int defaultValue = 0) {
			if (ints == null) {
				intValue = defaultValue;
				return false;
			}
			if (ints.TryGetValue(key, out intValue)) {
				return true;
			}
			return false;
		}

		public IEnumerable<string> IntKeys() {
			if (ints == null) {
				return Enumerable.Empty<string>();
			}
			return ints.Keys;
		}

		#endregion

		#region Bools

		public void SetBool(string key, bool value) {
			if (bools == null) {
				bools = new StringBoolDictionary();
			}
			bools[key] = value;
		}

		public bool GetBool(string key, bool defaultValue = false) {
			if (bools == null) {
				return defaultValue;
			}
			if (bools.TryGetValue(key, out var value)) {
				return value;
			}
			return defaultValue;
		}

		public bool HasBool(string key) {
			if (bools == null) {
				return false;
			}

			return bools.ContainsKey(key);
		}

		public void RemoveBool(string key) {
			bools?.Remove(key);
		}

		#endregion

		#region Floats

		public void SetFloat(string key, float value) {
			if (floats == null) {
				floats = new StringFloatDictionary();
			}
			floats[key] = value;
		}

		public float GetFloat(string key, float defaultValue = 0) {
			if (floats == null) {
				return defaultValue;
			}
			if (floats.TryGetValue(key, out var value)) {
				return value;
			}
			return defaultValue;
		}

		public bool TryGetFloat(string key, out float value) {
			value = 0;
			if (floats == null) {
				return false;
			}
			return floats.TryGetValue(key, out value);
		}

		public void RemoveFloat(string key) {
			floats?.Remove(key);
		}

		#endregion

		#region Strings

		public void SetString(string key, string value) {
			if (strings == null) {
				strings = new StringStringDictionary();
			}
			strings[key] = value;
		}

		public string GetString(string key, string defaultValue = "") {
			if (strings == null) {
				return defaultValue;
			}
			if (strings.TryGetValue(key, out var value)) {
				return value;
			}
			return defaultValue;
		}

		public void RemoveString(string key) {
			strings?.Remove(key);
		}

		#endregion

		#region Game Objects

		public void SetGameObject(string key, GameObject value) {
			if (gameObjects == null) {
				gameObjects = new StringGameObjectDictionary();
			}
			gameObjects[key] = value;
		}

		public GameObject GetGameObject(string key) {
			if (gameObjects == null) {
				return null;
			}
			if (gameObjects.TryGetValue(key, out var value)) {
				return value;
			}
			return null;
		}

		public bool TryGetGameObject(string key, out GameObject value) {
			if (gameObjects == null) {
				value = null;
				return false;
			}
			if (gameObjects.TryGetValue(key, out value)) {
				return true;
			}
			return false;
		}

		public void RemoveGameObject(string key) {
			gameObjects?.Remove(key);
		}

		#endregion

		#region Components

		public void SetComponent(string key, Component value) {
			if (components == null) {
				components = new StringComponentDictionary();
			}
			components[key] = value;
		}

		public Component GetComponent(string key) {
			if (components == null) {
				return null;
			}
			if (components.TryGetValue(key, out var value)) {
				return value;
			}
			return null;
		}

		public T GetComponent<T>(string key) where T : Component {
			if (components == null) {
				return null;
			}
			if (components.TryGetValue(key, out var value)) {
				return value as T;
			}
			return null;
		}

		public bool TryGetComponent(string key, out Component value) {
			if (components == null) {
				value = null;
				return false;
			}
			if (components.TryGetValue(key, out value)) {
				return true;
			}
			return false;
		}

		public void RemoveComponent(string key) {
			components?.Remove(key);
		}

		#endregion

		#region Unity Objects

		public void SetUnityObject(string key, UnityEngine.Object value) {
			if (unityObjects == null) {
				unityObjects = new StringUnityObjectDictionary();
			}
			unityObjects[key] = value;
		}

		public UnityEngine.Object GetUnityObject(string key) {
			if (unityObjects == null) {
				return null;
			}
			if (unityObjects.TryGetValue(key, out var value)) {
				return value;
			}
			return null;
		}

		public bool TryGetUnityObject(string key, out UnityEngine.Object value) {
			if (unityObjects == null) {
				value = null;
				return false;
			}
			if (unityObjects.TryGetValue(key, out value)) {
				return true;
			}
			return false;
		}

		public void RemoveUnityObject(string key) {
			unityObjects?.Remove(key);
		}

		#endregion

		#region System Objects

		public void SetObject(string key, object value) {
			if (objects == null) {
				objects = new StringObjectDictionary();
			}
			objects[key] = value;
		}

		public object GetObject(string key) {
			if (objects == null) {
				return null;
			}
			if (objects.TryGetValue(key, out var value)) {
				return value;
			}
			return null;
		}

		public bool TryGetObject(string key, out object value) {
			if (objects == null) {
				value = null;
				return false;
			}
			if (objects.TryGetValue(key, out value)) {
				return true;
			}
			return false;
		}

		public void RemoveObject(string key) {
			objects?.Remove(key);
		}

		#endregion

		#region Complex Set and Get

		public void Set(string key, object value) {
			var type = value.GetType();
			Set(key, value, type);
		}

		public void Set(string key, object value, Type type) {

			if (typeof(GameObject).IsAssignableFrom(type)) {
				var gameObject = value as GameObject;
				SetGameObject(key, gameObject);
			}
			else
			if (typeof(Component).IsAssignableFrom(type)) {
				var component = value as Component;
				SetComponent(key, component);
			}
			else
			if (typeof(UnityEngine.Object).IsAssignableFrom(type)) {
				var unityObject = value as UnityEngine.Object;
				SetUnityObject(key, unityObject);
			}
			else
			if (typeof(int).IsAssignableFrom(type) || typeof(Enum).IsAssignableFrom(type)) {
				var intValue = Convert.ToInt32(value);
				SetInt(key, intValue);
			}
			else
			if (typeof(float).IsAssignableFrom(type)) {
				var floatValue = Convert.ToSingle(value);
				SetFloat(key, floatValue);
			}
			else
			if (typeof(bool).IsAssignableFrom(type)) {
				var boolValue = Convert.ToBoolean(value);
				SetBool(key, boolValue);
			}
			else
			if (typeof(string).IsAssignableFrom(type)) {
				var stringValue = Convert.ToString(value);
				SetString(key, stringValue);
			}
			else
			if (typeof(object).IsAssignableFrom(type)) {
				SetObject(key, value);
			}
			else {
				// TODO: serialize all other types and store as strings

				throw new Exception($"Unable to store {type} on Blackboard");

			}
		}


		public T Get<T>(string key) {
			return (T)Get(key, typeof(T));
		}

		public object Get(string key, Type type) {

			if (typeof(GameObject).IsAssignableFrom(type)) {
				var gameObject = GetGameObject(key);
				return gameObject;
			}
			else
			if (typeof(Component).IsAssignableFrom(type)) {
				var component = GetComponent(key);
				return component;
			}
			else
			if (typeof(UnityEngine.Object).IsAssignableFrom(type)) {
				var unityObject = GetUnityObject(key);
				return unityObject;
			}
			else
			if (typeof(int).IsAssignableFrom(type) || typeof(Enum).IsAssignableFrom(type)) {
				return GetInt(key);
			}
			else
			if (typeof(float).IsAssignableFrom(type)) {
				return GetFloat(key);
			}
			else
			if (typeof(bool).IsAssignableFrom(type)) {
				return GetBool(key);
			}
			else
			if (typeof(string).IsAssignableFrom(type)) {
				return GetString(key);
			}
			else
			if (typeof(object).IsAssignableFrom(type)) {
				return GetObject(key);
			}
			throw new System.NotImplementedException();
		}

		public bool Remove<T>(string key) {
			return Remove(key, typeof(T));
		}

		public bool Remove(string key, Type type) {

			if (typeof(GameObject).IsAssignableFrom(type)) {
				RemoveGameObject(key);
			}
			else
			if (typeof(Component).IsAssignableFrom(type)) {
				RemoveComponent(key);
			}
			else
			if (typeof(UnityEngine.Object).IsAssignableFrom(type)) {
				RemoveUnityObject(key);
			}
			else
			if (typeof(int).IsAssignableFrom(type)) {
				RemoveInt(key);
			}
			else
			if (typeof(float).IsAssignableFrom(type)) {
				RemoveFloat(key);
			}
			else
			if (typeof(bool).IsAssignableFrom(type)) {
				RemoveBool(key);
			}
			else
			if (typeof(string).IsAssignableFrom(type)) {
				RemoveString(key);
			}
			else
			if (typeof(object).IsAssignableFrom(type)) {
				RemoveObject(key);
			}
			else {
				throw new System.NotImplementedException(type.ToString());
			}
			return true;
		}

		public T GetAndRemove<T>(string key) {
			var result = Get<T>(key);
			Remove<T>(key);
			return result;
		}

		public object GetAndRemove(string key, Type type) {
			var result = Get(key, type);
			Remove(key, type);
			return result;
		}

		#endregion

	}
}