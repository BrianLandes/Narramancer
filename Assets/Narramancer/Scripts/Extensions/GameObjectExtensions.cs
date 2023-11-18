
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Narramancer {
	public static class GameObjectExtensions {

		public static T[] FindObjectsOfType<T>(this Object @object, bool includeInactive = false) where T: Component {
			
			if (!includeInactive) {
				return Object.FindObjectsOfType<T>();
			}

#if UNITY_2020_3_OR_NEWER
			return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
			Scene scene = SceneManager.GetActiveScene();
			var rootObjects = scene.GetRootGameObjects();
			return rootObjects.SelectMany(x => x.GetComponentsInChildren<T>()).ToArray();
#endif
		}
	}
}