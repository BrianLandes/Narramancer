using UnityEngine;
using XNode;

namespace Narramancer {

	[CreateNodeMenu("Adjective/Add Relationship")]
	public class AddRelationshipNode : AbstractInstanceInputChainedRunnableNode {

		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField]
		RelationshipScriptableObject relationship = default;

		[Input(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField]
		NounInstance other = default;

		[SerializeField]
		[NodeEnum]
		SourceOrDestination sourceOrDestination = SourceOrDestination.Source;

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			var instance = GetInstance(runner.Blackboard);

			var relationship = GetInputValue(runner.Blackboard, nameof(this.relationship), this.relationship);

			if (instance != null || relationship != null) {
				var other = GetInputValue(runner.Blackboard, nameof(this.other), this.other);
				instance.AddRelationship(relationship, other, sourceOrDestination);
			}
			else {
				Debug.LogError("Instance or relationship was null", this);
			}

		}
	}
}