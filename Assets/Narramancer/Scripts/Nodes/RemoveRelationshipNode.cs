using UnityEngine;
using XNode;

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

			var other = GetInputValue<NounInstance>(runner.Blackboard, nameof(this.other));

			if (other != null) {
				instance.RemoveRelationship(relationship, other, requirement);
			}
			else {
				instance.RemoveRelationship(relationship, requirement);
			}


		}
	}
}