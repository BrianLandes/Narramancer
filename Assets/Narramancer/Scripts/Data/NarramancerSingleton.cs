
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Narramancer {

	public class NarramancerSingleton : Singleton<NarramancerSingleton> {

		[SerializeField]
		public bool runOnGameStart = true;

		[SerializeField]
		List<NounScriptableObject> nouns = new List<NounScriptableObject>();
		public List<NounScriptableObject> Nouns { get => nouns; set => nouns = value; }

		[SerializeField]
		List<AdjectiveScriptableObject> adjectives = new List<AdjectiveScriptableObject>();
		public static string AdjectivesFieldName => nameof(adjectives);
		public List<AdjectiveScriptableObject> Adjectives { get => adjectives; set => adjectives = value; }

		[SerializeField]
		List<ActionVerb> runAtStart = new List<ActionVerb>();
		public static string RunAtStartFieldName => nameof(runAtStart);
		public List<ActionVerb> RunAtStart { get => runAtStart; set => runAtStart = value; }

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

				foreach (var verb in runAtStart) {
					if (verb.TryGetFirstRunnableNodeAfterRootNode(out var runnableNode)) {
						var runner = CreateNodeRunner(verb.name);
						runner.Start(runnableNode);
					}
				}
			}
		}

		public override void OnUpdate() {
			UpdateTimers();
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

		public NodeRunner CreateNodeRunner(string name) {
			var runner = new NodeRunner();
			storyInstance.NodeRunners.Add(name, runner);
			return runner;
		}

		public NodeRunner GetNodeRunner(string name) {
			return storyInstance.NodeRunners[name];
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

		public NounInstance CreateInstance(IInstancable instancable) {
			return storyInstance.CreateInstance(instancable);
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
			this.storyInstance = storyInstance;

			if (storyInstance.sceneIndex >= 0) {
				var asyncOperation = SceneManager.LoadSceneAsync(storyInstance.sceneIndex, LoadSceneMode.Single);
				asyncOperation.completed += _ => {
					foreach (var monoBehaviour in monoBehaviourTable.ToArray()) {
						monoBehaviour.Deserialize(storyInstance);
					}

					storyInstance.SaveTable = null;
				};
			}
		}

	}

}

