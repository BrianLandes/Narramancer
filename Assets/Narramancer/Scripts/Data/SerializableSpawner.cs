using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Narramancer {

    public class SerializableSpawner : SerializableMonoBehaviour {

        [SerializeField]
        private GameObject prefab = default;

		[SerializeField]
		private Transform spawnLocation = default;

		private List<GameObject> spawns = new List<GameObject>();

		private void Start() {
			prefab.SetActive(false);
		}

		public GameObject Spawn() {
			var newSpawn = Instantiate(prefab, transform);
			newSpawn.SetActive(true);
			if (spawnLocation != null) {
				newSpawn.transform.position = spawnLocation.position;
				newSpawn.transform.rotation = spawnLocation.rotation;
			}
			newSpawn.name = newSpawn.name.Replace("(Clone)", $" ({Guid.NewGuid()})");
			spawns.Add(newSpawn);
			return newSpawn;
		}

		public void DestroyAll() {
			foreach( var spawn in spawns) {
				Destroy(spawn);
			}
			spawns.Clear();
		}

		public override void Serialize(StoryInstance story) {
			base.Serialize(story);

			spawns = spawns.Where(x => x != null).ToList();

			var spawnNames = spawns.Select(x => x.name).ToList();
			story.Blackboard.Set(Key("spawnNames"), spawnNames);
		}

		public override void Deserialize(StoryInstance story) {

			DestroyAll();

			base.Deserialize(story);

			var spawnNames = story.Blackboard.Get<List<string>>(Key("spawnNames"));

			foreach( var name in spawnNames) {
				var newSpawn = Spawn();
				newSpawn.name = name;
				var serializableMonoBehaviours = newSpawn.GetComponentsInChildren<ISerializableMonoBehaviour>();

				foreach( var monoBehaviour in serializableMonoBehaviours) {
					monoBehaviour.Deserialize(story);
				}
			}

		}
	}
}