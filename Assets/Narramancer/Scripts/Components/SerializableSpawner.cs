using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Narramancer {

	public class SerializableSpawner : SerializableMonoBehaviour {

		[SerializeField]
		private GameObject prefab = default;
		public static string PrefabFieldName => nameof(prefab);

		[Serializable]
		public enum SpawnLocationType {
			None,
			AtTransform,
			RandomInXYCircle,
			RandomInXZCircle,
			RandomInYZCircle,
			RandomInSphere,
			LayoutInLine,
			RandomInXYRect,
			RandomInXZRect,
			RandomInYZRect,
		}
		[SerializeField]
		private SpawnLocationType spawnLocationType = default;
		public static string SpawnLocationTypeFieldName => nameof(spawnLocationType);

		[SerializeField]
		private Transform spawnLocation = default;
		public static string SpawnLocationFieldName => nameof(spawnLocation);

		[SerializeField]
		private float circleRadius = default;
		public static string CircleRadiusFieldName => nameof(circleRadius);

		[SerializeField]
		private float rectWidth = default;
		public static string RectWidthFieldName => nameof(rectWidth);

		[SerializeField]
		private float rectHeight = default;
		public static string RectHeightFieldName => nameof(rectHeight);

		[SerializeField]
		private bool randomizeRotation = false;
		public static string RandomizeRotationFieldName => nameof(randomizeRotation);

		[SerializeField]
		private bool spawnForEachNoun = false;
		public static string SpawnForEachNounFieldName => nameof(spawnForEachNoun);


		private List<GameObject> spawns = new List<GameObject>();

#if UNITY_EDITOR
		[MenuItem("GameObject/Narramancer/Serializable Spawner", false, 10)]
		static void CreateGameObject(MenuCommand menuCommand) {

			GameObject gameObject = new GameObject("Spawner");
			gameObject.AddComponent<SerializableSpawner>();

			GameObjectUtility.SetParentAndAlign(gameObject, menuCommand.context as GameObject);

			Undo.RegisterCreatedObjectUndo(gameObject, "Create " + gameObject.name);
			Selection.activeObject = gameObject;
		}
#endif



		private void Start() {
			prefab.SetActive(false);

			#region Spawn for any existing nouns
			if (!valuesOverwrittenByDeserialize && spawnForEachNoun) {
				foreach (var instance in NarramancerSingleton.Instance.GetInstances()) {
					var newGameObject = Spawn();
					instance.GameObject = newGameObject;
				}
			}
			if (spawnForEachNoun) {
				NarramancerSingleton.Instance.OnCreateInstance += OnCreateInstance;
			}
			#endregion
		}

		private void OnCreateInstance(NounInstance instance) {
			var newGameObject = Spawn();
			instance.GameObject = newGameObject;
		}

		public override void OnDestroy() {
			base.OnDestroy();
			NarramancerSingleton.Instance.OnCreateInstance -= OnCreateInstance;
		}

		public GameObject Spawn() {
			var newSpawn = Instantiate(prefab, transform);
			newSpawn.SetActive(true);
			switch (spawnLocationType) {
				case SpawnLocationType.None:
					break;
				case SpawnLocationType.AtTransform:
					if (spawnLocation != null) {
						newSpawn.transform.position = spawnLocation.position;
						newSpawn.transform.rotation = spawnLocation.rotation;
					}
					break;
				case SpawnLocationType.RandomInXYCircle: {
						var center = spawnLocation != null ? spawnLocation : transform;
						var variance = UnityEngine.Random.insideUnitCircle * circleRadius;
						newSpawn.transform.position = center.position + new Vector3(variance.x, variance.y, 0);
					}
					break;
				case SpawnLocationType.RandomInXZCircle: {
						var center = spawnLocation != null ? spawnLocation : transform;
						var variance = UnityEngine.Random.insideUnitCircle * circleRadius;
						newSpawn.transform.position = center.position + new Vector3(variance.x, 0, variance.y);
					}
					break;
				case SpawnLocationType.RandomInYZCircle: {
						var center = spawnLocation != null ? spawnLocation : transform;
						var variance = UnityEngine.Random.insideUnitCircle * circleRadius;
						newSpawn.transform.position = center.position + new Vector3(0, variance.x, variance.y);
					}
					break;
				case SpawnLocationType.RandomInSphere: {
						var center = spawnLocation != null ? spawnLocation : transform;
						newSpawn.transform.position = center.position + UnityEngine.Random.insideUnitSphere * circleRadius;
					}
					break;
				case SpawnLocationType.LayoutInLine: {
						var center = spawnLocation != null ? spawnLocation : transform;
						var position = center.position;
						foreach (var spawn in spawns) {
							if (spawn == null) {
								continue;
							}
							spawn.transform.position = position;

							position += transform.forward * circleRadius;
						}
						newSpawn.transform.position = position;
					}
					break;
				case SpawnLocationType.RandomInXYRect: {
						var center = spawnLocation != null ? spawnLocation : transform;
						var widthVariance = Probabilititties.Gaussian() * 0.33f * rectWidth;
						var heightVariance = Probabilititties.Gaussian() * 0.33f * rectHeight;
						newSpawn.transform.position = center.position + new Vector3(widthVariance, heightVariance, 0f);
					}
					break;
				case SpawnLocationType.RandomInXZRect: {
						var center = spawnLocation != null ? spawnLocation : transform;
						var widthVariance = Probabilititties.Gaussian() * 0.33f * rectWidth;
						var heightVariance = Probabilititties.Gaussian() * 0.33f * rectHeight;
						newSpawn.transform.position = center.position + new Vector3(widthVariance, 0f, heightVariance);
					}
					break;
				case SpawnLocationType.RandomInYZRect: {
						var center = spawnLocation != null ? spawnLocation : transform;
						var widthVariance = Probabilititties.Gaussian() * 0.33f * rectWidth;
						var heightVariance = Probabilititties.Gaussian() * 0.33f * rectHeight;
						newSpawn.transform.position = center.position + new Vector3(0f, widthVariance, heightVariance);
					}
					break;
			}

			if (randomizeRotation) {
				newSpawn.transform.rotation = Quaternion.Euler(0, UnityEngine.Random.Range(0, 360), 0);
			}

			newSpawn.name = newSpawn.name.Replace("(Clone)", $" ({Guid.NewGuid()})");
			spawns.Add(newSpawn);
			return newSpawn;
		}

		public void DestroyAll() {
			foreach (var spawn in spawns) {
				Destroy(spawn);
			}
			spawns.Clear();
		}

		public List<GameObject> GetAllGameObjects() {
			spawns = spawns.Where(x => x != null).ToList();
			return spawns;
		}

		public override void Serialize(StoryInstance story) {
			base.Serialize(story);

			spawns = spawns.Where(x => x != null).ToList();

			var spawnNames = spawns.Select(x => x.name).ToList();
			story.SaveTable.Set(Key("spawnNames"), spawnNames);
		}

		public override void Deserialize(StoryInstance story) {

			DestroyAll();

			base.Deserialize(story);

			var spawnNames = story.SaveTable.GetAndRemove<List<string>>(Key("spawnNames"));

			foreach (var name in spawnNames) {
				var newSpawn = Spawn();
				newSpawn.name = name;
				var serializableMonoBehaviours = newSpawn.GetComponentsInChildren<ISerializableMonoBehaviour>();

				foreach (var monoBehaviour in serializableMonoBehaviours) {
					monoBehaviour.Deserialize(story);
				}
			}

		}
	}
}