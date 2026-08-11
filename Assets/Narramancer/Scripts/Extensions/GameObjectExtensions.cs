
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Narramancer {
	public static class GameObjectExtensions {

		public static T[] FindObjectsOfType<T>(bool includeInactive = false) where T : Component {

#if UNITY_6000_0_OR_NEWER
			// Unity 6 deprecated FindObjectsSortMode along with every overload that takes one: InstanceID is
			// being replaced by EntityId, and Unity states the previous sort order cannot be maintained. So
			// results are unordered, and callers must not depend on which match comes first — the
			// FirstOrDefault callers (ChoicePrinter, PrintTextNode, SerializableVariableReference) are only
			// correct because they either filter to a unique match or genuinely accept any one.
			return Object.FindObjectsByType<T>(includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude);
#elif UNITY_2021_3_OR_NEWER
			return Object.FindObjectsByType<T>(
				includeInactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude,
				FindObjectsSortMode.None);
#else
			if (!includeInactive) {
				return Object.FindObjectsOfType<T>();
			}
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