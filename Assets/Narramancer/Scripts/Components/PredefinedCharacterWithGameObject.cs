using System;
using System.Collections.Generic;
using UnityEngine;

namespace Narramancer {
	public class PredefinedCharacterWithGameObject : SerializableMonoBehaviour, IInstancable {

		[SerializeField]
		private string displayName = "";
		public string DisplayName => displayName;

		[SerializeField]
		private NounType nounType = NounType.Character;
		public NounType NounType => nounType;

		[SerializeField]
		private NounUID uid = new NounUID();
		public NounUID ID => uid;

		[SerializeField]
		private Pronouns pronouns = Pronouns.Nonbinary;
		public Pronouns Pronouns => pronouns;

		[SerializeField]
		private List<PropertyAssignment> properties = new List<PropertyAssignment>();
		public IEnumerable<PropertyAssignment> Properties => properties;

		[SerializeField]
		private List<StatAssignment> stats = new List<StatAssignment>();
		public IEnumerable<StatAssignment> Stats => stats;

		[SerializeField]
		private List<RelationshipAssignment> relationships = new List<RelationshipAssignment>();
		public IEnumerable<RelationshipAssignment> Relationships => relationships;

		[SerializeField]
		private Blackboard startingBlackboard = new Blackboard();
		public Blackboard Blackboard => startingBlackboard;

		public NounInstance Instance { get; private set; }
		private void Start() {
			if ( !valuesOverwrittenByDeserialize) {
				Instance = NarramancerSingleton.Instance.CreateInstance(this);
				Instance.GameObject = gameObject;
			}
		}

		public override void Serialize(StoryInstance map) {
			base.Serialize(map);

			map.Blackboard.Set(Key("Instance"), Instance);
		}

		public override void Deserialize(StoryInstance map) {
			base.Deserialize(map);

			Instance = map.Blackboard.Get<NounInstance>(Key("Instance"));
			Instance.GameObject = gameObject;
		}
	}

}
