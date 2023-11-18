using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Narramancer {
	[Serializable]
	public class StoryInstance {

		/// <summary>
		/// All Instances that the story has.
		/// </summary>
		[SerializeField]
		private List<NounInstance> instances = new List<NounInstance>();
		public List<NounInstance> Instances => instances;

		[SerializeField]
		private Blackboard blackboard = new Blackboard();
		public Blackboard Blackboard => blackboard;

		[SerializeField]
		private List<SerializableTimer> timers = new List<SerializableTimer>();
		public List<SerializableTimer> Timers => timers;

		[SerializeField]
		private StringNodeRunnerDictionary nodeRunners = new StringNodeRunnerDictionary();
		public StringNodeRunnerDictionary NodeRunners => nodeRunners;

		[SerializeField]
		private StringPromiseDictionary promises = new StringPromiseDictionary();
		public StringPromiseDictionary Promises => promises;

		[SerializeField]
		private FlagIntDictionary flags = new FlagIntDictionary();
		public FlagIntDictionary Flags => flags;

		[SerializeField]
		public int sceneIndex = -1;

		public StoryInstance() { }

		public StoryInstance(IEnumerable<NounScriptableObject> nouns) {
			instances.Clear();

			foreach (var typicalNoun in nouns) {
				var instance = new NounInstance(typicalNoun);
				instances.Add(instance);
			}

			// Establish Relationships
			// Must occur after all initial instances have been created
			foreach (var typicalNoun in nouns) {
				if (TryGetInstance(typicalNoun, out var instance)) {
					instance.AddRelationships(typicalNoun.Relationships, instances);
				}
			}
		}

		public void Clear() {
			instances.Clear();
			blackboard.Clear();
			timers.Clear();
			nodeRunners.Clear();
			promises.Clear();
			flags.Clear();
		}

		public bool TryGetInstance(NounScriptableObject noun, out NounInstance instance) {
			instance = GetInstance(noun);
			return instance != null;
		}

		public NounInstance GetInstance(NounScriptableObject noun) {
			return instances.FirstOrDefault(instance => instance.Noun == noun);
		}

		public NounInstance CreateInstance(IInstancable instancable) {
			var instance = new NounInstance(instancable);
			instances.Add(instance);
			instance.AddRelationships(instancable.Relationships, instances);
			return instance;
		}

	}
}
