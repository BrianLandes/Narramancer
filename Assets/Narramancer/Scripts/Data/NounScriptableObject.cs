using System.Collections.Generic;
using UnityEngine;

namespace Narramancer {

	[CreateAssetMenu(menuName = "Narramancer/Noun (Character, Item, Location, etc)", fileName = "New Noun")]
	public class NounScriptableObject : ScriptableObject, IInstancable {

		[SerializeField]
		private NounUID uid = new NounUID();
		public NounUID ID => uid;

		[SerializeField]
		private ToggleableString displayName = new ToggleableString(false);
		public string DisplayName => displayName.activated ? displayName.value : name;

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
		public Blackboard Blackboard {
			get {
				var blackboard = startingBlackboard.Copy();
				foreach (var assignment in BlackboardAssignments) {
					var value = assignment.Assignment.GetValue();
					blackboard.Set(assignment.Name, value);
				}
				return blackboard;
			}
		}

		[SerializeField]
		private List<NarramancerPortWithAssignment> blackboardAssignments = new List<NarramancerPortWithAssignment>();
		public List<NarramancerPortWithAssignment> BlackboardAssignments => blackboardAssignments;

#if UNITY_EDITOR
		private void OnValidate() {
			var allUids = new HashSet<string>();

			var allNounScriptableObjects = PseudoEditorUtilities.GetAllInstances<NounScriptableObject>();

			foreach (var noun in allNounScriptableObjects) {
				if (allUids.Contains(noun.uid.ToString())) {
					noun.uid.GenerateNew();
				}
				allUids.Add(noun.uid.ToString());
			}
		}
#endif
	}
}