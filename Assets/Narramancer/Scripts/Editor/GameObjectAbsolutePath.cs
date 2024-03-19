using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Narramancer {
    public static class GameObjectAbsolutePath {

        private static string SCENE_DELIMITER = "<*>";
        private static string GAMEOBJECT_DELIMITER = "</>";


		private static string FullPath(GameObject gameObject) {
			var parent = gameObject.transform.parent;
			if (parent != null) {
				var parentName = FullPath(parent.gameObject);
				return $"{parentName}{GAMEOBJECT_DELIMITER}{gameObject.name}";
			}
			return gameObject.name;
		}

		public static string Get(GameObject gameObject) {
			return $"{gameObject.scene.name}{SCENE_DELIMITER}{FullPath(gameObject)}";
		}

		public static bool TryGetGameObject(string absolutePath, out GameObject gameObject) {
			var activeScene = SceneManager.GetActiveScene();
			var split = absolutePath.Split(SCENE_DELIMITER);
			var sceneName = split[0];
			if (activeScene.name == sceneName) {
				var fullPath = split[1].Split(GAMEOBJECT_DELIMITER);

				if (fullPath.Length > 0) {
					bool NameMatchesIndex(GameObject gameObject, int index) {
						return fullPath[index] == gameObject.name;
					}

					IEnumerable<GameObject> GetChildren(GameObject gameObject) {
						for (int ii = 0; ii < gameObject.transform.childCount; ii++) {
							yield return gameObject.transform.GetChild(ii).gameObject;
						}
					}

					GameObject NextChildInGameObjects(IEnumerable<GameObject> gameObjects, int index) {
						var child = gameObjects.FirstOrDefault(gameObject => NameMatchesIndex(gameObject, index));
						return child;
					}

					IEnumerable<GameObject> gameObjects = activeScene.GetRootGameObjects();

					gameObject = NextChildInGameObjects(gameObjects, 0);
					
					for (int ii = 1; ii < fullPath.Length; ii++) {
						gameObjects = GetChildren(gameObject);
						gameObject = NextChildInGameObjects(gameObjects, ii);
						if (gameObject == null) {
							return false;
						}
					}

					return gameObject != null;
				}
			}

			gameObject = null;
			return false;
		}
	}
}