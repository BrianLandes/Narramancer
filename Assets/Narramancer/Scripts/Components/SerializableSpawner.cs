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

		/// <summary>
		/// What was recorded about one spawned object, so the replacement can be given the same identity.
		/// <para>
		/// The guids are the important part. This used to save only the name, and restore identity with
		/// <c>newSpawn.name = name</c> — which worked because save keys contained
		/// <c>transform.FullPath()</c>, and the path contains the name. Once identity became a guid, restoring
		/// the name restored nothing: every re-spawned object would have minted a fresh guid and silently
		/// failed to find any of its saved state.
		/// </para>
		/// </summary>
		[Serializable]
		public class SpawnRecord {
			public string name;

			/// <summary>Every <see cref="Saveable"/> in the spawn's hierarchy, in a fixed order.</summary>
			public List<string> guids = new List<string>();
		}

		public override void Serialize(StoryInstance story) {
			base.Serialize(story);

			spawns = spawns.Where(x => x != null).ToList();

			var records = spawns.Select(spawn => new SpawnRecord() {
				name = spawn.name,
				guids = spawn.GetComponentsInChildren<Saveable>(true).Select(saveable => saveable.Guid).ToList()
			}).ToList();

			story.SaveTable.Set(Key("spawns"), records);
		}

		public override void Deserialize(StoryInstance story) {

			DestroyAll();

			base.Deserialize(story);

			var records = story.SaveTable.GetAndRemove<List<SpawnRecord>>(Key("spawns"));
			if (records == null) {
				return;
			}

			foreach (var record in records) {
				var newSpawn = Spawn();
				newSpawn.name = record.name;

				// Identity before state: the components below key their saved values off these guids, so the
				// replacement has to be answering to the right ones before anything is read back.
				var saveables = newSpawn.GetComponentsInChildren<Saveable>(true);
				for (var i = 0; i < saveables.Length && i < record.guids.Count; i++) {
					saveables[i].AssignGuid(record.guids[i]);
				}
				if (saveables.Length != record.guids.Count) {
					Debug.LogWarning(
						$"'{record.name}' was saved with {record.guids.Count} saveable object(s) but its prefab now has {saveables.Length}. " +
						"The prefab changed since the save was written, so some of its state will not be restored.", this);
				}

				foreach (var monoBehaviour in newSpawn.GetComponentsInChildren<ISerializableMonoBehaviour>()) {
					monoBehaviour.Deserialize(story);
				}
			}

		}
	}
}