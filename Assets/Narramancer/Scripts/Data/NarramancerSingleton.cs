
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Narramancer {

	public class NarramancerSingleton : Singleton<NarramancerSingleton> {

		[SerializeField]
		public bool runOnGameStart = true;

		[SerializeField]
		NounScriptableObjectList nouns = new NounScriptableObjectList();
		public List<NounScriptableObject> Nouns => nouns.list;
		public static string NounsFieldName => nameof(nouns);

		[SerializeField]
		List<AdjectiveScriptableObject> adjectives = new List<AdjectiveScriptableObject>();
		public static string AdjectivesFieldName => nameof(adjectives);
		public List<AdjectiveScriptableObject> Adjectives { get => adjectives; set => adjectives = value; }

		[SerializeField]
		ActionVerbList runAtStart = new ActionVerbList();
		public static string RunAtStartFieldName => nameof(runAtStart);

		[SerializeField]
		List<VerbGraph> recentlyOpenedGraphs = new List<VerbGraph>();
		public static string RecentlyOpenedGraphsFieldName => nameof(recentlyOpenedGraphs);
		public List<VerbGraph> RecentlyOpenedGraphs { get => recentlyOpenedGraphs; set => recentlyOpenedGraphs = value; }


		[SerializeField]
		private List<NarramancerPortWithAssignment> globalVariables = new List<NarramancerPortWithAssignment>();
		public static string GlobalVariablesFieldName => nameof(globalVariables);
		public List<NarramancerPortWithAssignment> GlobalVariables => globalVariables;

		[SerializeField]
		private StoryInstance storyInstance;
		public StoryInstance StoryInstance => storyInstance;
		public static string StoryInstanceFieldName => nameof(storyInstance);

		private HashSet<ISerializableMonoBehaviour> monoBehaviourTable = new HashSet<ISerializableMonoBehaviour>();

		[SerializeField]
		public int maxNodesRunPerFrame = 100;

		public event Action<NounInstance> OnCreateInstance;

		public event Action PreUpdateActions;

		public override void OnPreprocessBuild() {
			Clear();
		}

		public override void Initialize() {
			Clear();
		}

		public override void OnGameStart() {

			if (runOnGameStart) {
				storyInstance = new StoryInstance(Nouns);

				globalVariables.ApplyAssignmentsToBlackboard(storyInstance.Blackboard);

				foreach (var verb in runAtStart.list) {
					if (verb.TryGetFirstRunnableNodeAfterRootNode(out var runnableNode)) {
						var runner = CreateNodeRunner(verb.name);
						runner.Start(runnableNode);
					}
				}
			}
		}

		public override void OnUpdate() {
			PreUpdateActions?.Invoke();
			PreUpdateActions = null;
			if (storyInstance != null) {
				UpdateTimers();
				UpdateNodeRunners();
			}

		}

		#region Timers

		private void UpdateTimers() {

			foreach (var timer in storyInstance.Timers.ToList()) {

				if (timer.timeStamp <= Time.time) {
					timer.promise.Resolve();
					storyInstance.Timers.Remove(timer);
				}
			}
		}

		public Promise MakeTimer(float duration) {
			var timeStamp = Time.time + duration;
			var promise = new Promise();
			var timer = new SerializableTimer() {
				timeStamp = timeStamp,
				promise = promise
			};
			storyInstance.Timers.Add(timer);
			return promise;
		}

		#endregion

		#region Node Runners

		private void UpdateNodeRunners() {
			var runs = 0;
			var didAnyUpdates = true;
			while (runs < maxNodesRunPerFrame && didAnyUpdates) {
				didAnyUpdates = false;
				foreach (var pair in storyInstance.NodeRunners.ToArray()) {
					var updated = pair.Value.Update();
					if (updated) {
						runs += 1;
						didAnyUpdates = true;
					}
				}
			}

		}

		public NodeRunner CreateNodeRunner(string name) {
			var runner = new NodeRunner();
			runner.name = name;
			storyInstance.NodeRunners.Add(name, runner);
			return runner;
		}

		public NodeRunner GetNodeRunner(string name) {
			if (storyInstance == null) {
				return null;
			}
			if (storyInstance.NodeRunners.TryGetValue(name, out var nodeRunner)) {
				return nodeRunner;
			}
			return null;
		}

		public void ReleaseNodeRunner(NodeRunner runner) {
			foreach (var pair in storyInstance.NodeRunners.ToArray()) {
				if (pair.Value == runner) {
					storyInstance.NodeRunners.Remove(pair.Key);
					break;
				}
			}
		}

		#endregion

		#region Flag

		/// <summary> Sets the flag's value to 'true'/'raised'/'1' </summary>
		public void RaiseFlag(Flag flag) {
			if (storyInstance.Flags.TryGetValue(flag, out var value)) {
				storyInstance.Flags[flag] = value + 1;
			}
			else {
				storyInstance.Flags[flag] = 1;
			}
		}

		public bool IsFlagRaised(Flag flag) {
			if (storyInstance.Flags.TryGetValue(flag, out var value)) {
				return value >= 1;
			}
			return false;
		}

		public void SetFlag(Flag flag, int value) {
			storyInstance.Flags[flag] = value;
		}

		public int GetFlag(Flag flag) {
			if (storyInstance.Flags.TryGetValue(flag, out var value)) {
				return value;
			}
			return 0;
		}

		#endregion

		#region Noun Instances

		public bool TryGetInstance(NounScriptableObject noun, out NounInstance instance) {
			instance = GetInstance(noun);
			return instance != null;
		}

		public NounInstance GetInstance(NounScriptableObject noun) {
			return storyInstance.Instances.FirstOrDefault(instance => instance.Noun == noun);
		}

		public NounInstance GetInstance(NounUID uid) {
			return storyInstance.Instances.FirstOrDefault(instance => instance.UID == uid);
		}

		public List<NounInstance> GetInstances() {
			return storyInstance.Instances;
		}

		[NonSerialized]
		private Dictionary<NounInstancesQuery, List<NounInstance>> queryInstancesTable = new Dictionary<NounInstancesQuery, List<NounInstance>>();

		public List<NounInstance> GetInstances(NounInstancesQuery query) {
			if (queryInstancesTable.TryGetValue(query, out var instances)) {
				return instances;
			}

			bool HasAllMustHaveProperties(NounInstance instance) {
				return query.mustHaveProperties.All(property => instance.HasProperty(property));
			}

			bool DoesNotHaveMustNotHaveProperties(NounInstance instance) {
				return query.mustNotHaveProperties.All(property => !instance.HasProperty(property));
			}
			var newResultList = storyInstance.Instances.Where(x => HasAllMustHaveProperties(x) && DoesNotHaveMustNotHaveProperties(x)).ToList();
			queryInstancesTable[query] = newResultList;
			return newResultList;
		}

		public void ClearQueryInstancesTable() {
			queryInstancesTable.Clear();
		}

		public NounInstance CreateInstance(IInstancable instancable) {
			var instance = storyInstance.CreateInstance(instancable);
			queryInstancesTable.Clear();
			OnCreateInstance?.Invoke(instance);
			return instance;
		}

		public void RemoveInstance(NounInstance instance) {
			if (instance != null) {
				storyInstance.Instances.Remove(instance);

				foreach (var relationship in instance.Relationships.ToArray()) {
					instance.RemoveRelationship(relationship);
				}

				if (instance.HasGameObject) {
					Destroy(instance.GameObject);
				}

				queryInstancesTable.Clear();
			}
		}

		#endregion

		public void Register(ISerializableMonoBehaviour monoBehaviour) {
			monoBehaviourTable.Add(monoBehaviour);
		}

		public void Unregister(ISerializableMonoBehaviour monoBehaviour) {
			monoBehaviourTable.Remove(monoBehaviour);
		}

		public void Clear() {
			storyInstance.Clear();
			monoBehaviourTable.Clear();
		}

		public StoryInstance PrepareStoryForSave() {

			var scene = SceneManager.GetActiveScene();
			storyInstance.sceneIndex = scene.buildIndex;

			storyInstance.SaveTable = new Blackboard();

			foreach (var monoBehaviour in monoBehaviourTable) {
				monoBehaviour.Serialize(storyInstance);
			}

			return storyInstance;
		}

		public void CleanUpStoryAfterSave() {
			storyInstance.SaveTable = null;
		}

		public void LoadStory(StoryInstance storyInstance) {
			PreUpdateActions += () => {
				this.storyInstance = null;

				if (storyInstance.sceneIndex >= 0) {
					var asyncOperation = SceneManager.LoadSceneAsync(storyInstance.sceneIndex, LoadSceneMode.Single);
					asyncOperation.completed += _ => {
						foreach (var monoBehaviour in monoBehaviourTable.ToArray()) {
							monoBehaviour.Deserialize(storyInstance);
						}

						storyInstance.SaveTable = null;
						ClearQueryInstancesTable();
						this.storyInstance = storyInstance;
					};
				}
			};
		}

	}

}

