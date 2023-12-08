
using System;
using UnityEngine;
using Object = UnityEngine.Object;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif
namespace Narramancer {
	public static class PseudoEditorUtilities {

#if UNITY_EDITOR

		public static T GetFirstInstance<T>() where T : ScriptableObject {
			string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);
			if (guids.Length == 0) {
				throw new SystemException($"Could not find {typeof(T).Name}");
			}
			string path = AssetDatabase.GUIDToAssetPath(guids[0]);

			return AssetDatabase.LoadAssetAtPath<T>(path);
		}

		public static ScriptableObject GetFirstInstance(Type type) {
			string[] guids = AssetDatabase.FindAssets("t:" + type.Name);
			if (guids.Length == 0) {
				return null;
			}
			string path = AssetDatabase.GUIDToAssetPath(guids[0]);

			return AssetDatabase.LoadAssetAtPath(path, type) as ScriptableObject;
		}

		public static T[] GetAllInstances<T>() where T : ScriptableObject {
			string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name);  //FindAssets uses tags check documentation for more info
			var instances = new List<T>();
			for (int i = 0; i < guids.Length; i++) {
				string path = AssetDatabase.GUIDToAssetPath(guids[i]);
				var assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path).OfType<T>();
				instances.AddRange(assets);
				var asset = AssetDatabase.LoadAssetAtPath<T>(path);
				if (asset != null && !instances.Contains(asset)) {

					instances.Add(asset);
				}
			}

			return instances.ToArray();
		}

		public static Object[] GetAllInstances(Type[] types) {
			string[] guids = AssetDatabase.FindAssets("");
			var instances = new List<Object>();
			for (int i = 0; i < guids.Length; i++) {
				string path = AssetDatabase.GUIDToAssetPath(guids[i]);
				var assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(path);
				assets = assets.WithoutNulls().Where(x => x.GetType().IsAssignableFrom(types)).ToArray();
				instances.AddRange(assets);
				var asset = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
				if (asset != null && !instances.Contains(asset) && asset.GetType().IsAssignableFrom(types)) {

					instances.Add(asset);
				}
			}

			return instances.ToArray();
		}

		public static IEnumerable<GameObject> GetAllPrefabsWithComponent<T>() where T : MonoBehaviour {
			string[] guids = AssetDatabase.FindAssets("t: prefab");
			var instances = new List<GameObject>();
			foreach (var guid in guids) {
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

				if (prefab.GetComponentInChildren<T>() != null) {
					instances.Add(prefab);
				}
			}

			return instances;
		}

		public static bool IsObjectAnAsset(Object thisObject) {
			var thisPath = AssetDatabase.GetAssetPath(thisObject);
			return !string.IsNullOrEmpty(thisPath);
		}

		public static bool IsObjectAnChild(Object thisObject) {
			return AssetDatabase.IsSubAsset(thisObject);
		}

		public static bool IsObjectAChildOfParent(Object subObject, Object parentObject) {
			if (!AssetDatabase.IsSubAsset(subObject)) {
				return false;
			}

			var parentPath = AssetDatabase.GetAssetPath(parentObject);
			var childPath = AssetDatabase.GetAssetPath(subObject);

			return parentPath.Equals(childPath);
		}

		public static T CreateAndAddChild<T>(Object parentObject, string name = null) where T : ScriptableObject {
			var child = ScriptableObject.CreateInstance<T>();
			child.name = name ?? $"{typeof(T).Name.Nicify()}";
			if (IsObjectAnAsset(parentObject)) {
				AddAsset(child, parentObject);
			}
			return child;
		}

		public static ScriptableObject CreateAndAddChild(Type childType, Object parentObject, string name = null) {
			var child = ScriptableObject.CreateInstance(childType);
			child.name = name ?? $"{childType.Name.Nicify()}";
			AddAsset(child, parentObject);
			return child;
		}

		public static void AddAsset(Object objectToAdd, Object parent) {
			AssetDatabase.AddObjectToAsset(objectToAdd, parent);
			EditorUtility.SetDirty(parent);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		public static void RenameAsset(Object asset, string newName) {

			string assetPath = AssetDatabase.GetAssetPath(asset);

			if (AssetDatabase.IsMainAsset(asset)) {

				AssetDatabase.RenameAsset(assetPath, newName);
			}
			else {
				//RenameAsset() only will rename Main Asset

				asset.name = newName;
			}

			EditorUtility.SetDirty(asset);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

		public static T CreateAsset<T>(string assetPath) where T : ScriptableObject {
			var @object = ScriptableObject.CreateInstance<T>();
			CreateAsset(@object, assetPath);
			return @object;
		}

		public static void CreateAsset(Object @object, string assetPath) {
			assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);
			AssetDatabase.CreateAsset(@object, assetPath);
			EditorUtility.SetDirty(@object);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}

#endif

	}
}