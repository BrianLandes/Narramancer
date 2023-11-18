using System.Collections.Generic;
using UnityEngine;

namespace Narramancer {

	[CreateAssetMenu(menuName="Narramancer/Noun (Character, Item, Location, etc)", fileName = "New Noun")]
	public class NounScriptableObject : ScriptableObject, IInstancable {

		[SerializeField]
		private NounUID uid = new NounUID();
		public NounUID ID => uid;

		[SerializeField]
		private ToggleableString displayName = new ToggleableString(false);
		public string DisplayName => displayName.activated ? displayName.value : name;

		[SerializeField]
		private NounType nounType = NounType.Character;
		public NounType NounType => nounType;
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

	}
}