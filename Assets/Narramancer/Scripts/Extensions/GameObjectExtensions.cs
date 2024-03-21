
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Narramancer {
	public static class GameObjectExtensions {

		public static T[] FindObjectsOfType<T>(bool includeInactive = false) where T : Component {

			if (!includeInactive) {
				return Object.FindObjectsOfType<T>();
			}

#if UNITY_2021_3_OR_NEWER
			return Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
			Scene scene = SceneManager.GetActiveScene();
			var rootObjects = scene.GetRootGameObjects();
			return rootObjects.SelectMany(x => x.GetComponentsInChildren<T>()).ToArray();
#endif
		}

		public static T FindAnyObjectByType<T>(bool includeInactive = false) where T : Component {

#if UNITY_2021_3_OR_NEWER
			if (includeInactive) {
				return Object.FindAnyObjectByType<T>(FindObjectsInactive.Include);
			}
			else {
				return Object.FindAnyObjectByType<T>(FindObjectsInactive.Exclude);
			}
#else
			Scene scene = SceneManager.GetActiveScene();
			var rootObjects = scene.GetRootGameObjects();
			return rootObjects.SelectMany(x => x.GetComponentsInChildren<T>()).Where(x=> x.gameObject.activeSelf || includeInactive).FirstOrDefault();
#endif
		}
	}
}