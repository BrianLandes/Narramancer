using UnityEngine;

namespace Narramancer {
	[CreateNodeMenu("Adjective/Remove Relationship")]
	public class RemoveRelationshipNode : AbstractInstanceInputChainedRunnableNode {

		[SerializeField]
		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		[HideLabel]
		private RelationshipScriptableObject relationshipNoun = default;

		[SerializeField]
		[Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited, backingValue = ShowBackingValue.Never)]
		private NounInstance other = default;

		[SerializeField]
		private RelationshipRequirement requirement = RelationshipRequirement.Either;

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			var instance = GetInstance(runner.Blackboard);
			if (instance == null)
				return; // TODO: warning?

			var relationship = GetInputValue(runner.Blackboard, nameof(relationshipNoun), relationshipNoun);
			if (relationship == null)
				return; // TODO: warning?

			var other = GetInputValue(runner.Blackboard, nameof(this.other), this.other);

			if (other != null) {
				instance.RemoveRelationship(relationship, other, requirement);
			}
			else {
				// TODO: remove all relationships of the given type
			}


		}
	}
}