using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Narramancer {

	public class SerializableSpawner : SerializableMonoBehaviour {

		[SerializeField]
		private GameObject prefab = default;

		[Serializable]
		public enum SpawnLocationType {
			None,
			AtTransform,
			RandomInXYCircle,
			RandomInXZCircle,
			RandomInYZCircle,
			RandomInSphere,
		}
		[SerializeField]
		private SpawnLocationType spawnLocationType = default;

		[SerializeField]
		private Transform spawnLocation = default;

		[SerializeField]
		private float circleRadius = default;

		private List<GameObject> spawns = new List<GameObject>();

		private void Start() {
			prefab.SetActive(false);
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