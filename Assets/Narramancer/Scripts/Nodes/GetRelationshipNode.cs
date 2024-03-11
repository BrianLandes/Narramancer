using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("Adjective/Get Relationship")]
	public class GetRelationshipNode : AbstractInstanceInputNode {

		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField, HideLabel]
		public RelationshipScriptableObject relationship = default;

		[Input(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField]
		public NounInstance other = default;

		[SerializeField]
		RelationshipRequirement relationshipRequirement = RelationshipRequirement.Either;

		[Output(ShowBackingValue.Never)]
		[SerializeField]
		private bool hasRelationship = false;

		[Output(ShowBackingValue.Never)]
		[SerializeField]
		private RelationshipInstance relaionshipInstance = default;

		[Output(ShowBackingValue.Never)]
		[SerializeField]
		private NounInstance otherInstance = default;

		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying) {

				var inputInstance = GetInstance(context);
				var other = GetInputValue<NounInstance>(context, nameof(this.other), null);
				RelationshipInstance GetRelationship() {

					if (inputInstance != null) {

						var relationship = GetInputValue(context, nameof(this.relationship), this.relationship);

						if (relationship != null) {

							if (other != null) {
								return inputInstance.GetRelationship(relationship, other, relationshipRequirement);
							}
							else {
								return inputInstance.GetRelationship(relationship, relationshipRequirement);
							}
						}
						else {
							Debug.LogError("Relationship was null", this);
							return null;
						}
					}
					else {
						Debug.LogError("Instance was null", this);
						return null;
					}
				}

				var relationshipInstance = GetRelationship();

				switch (port.fieldName) {
					case nameof(hasRelationship):
						return relationshipInstance != null;
					case nameof(relaionshipInstance):
						return relationshipInstance;
					case nameof(otherInstance):
						if (other != null) {
							return other;
						}

						return relationshipInstance.GetOther(inputInstance);
				}

			}
			return base.GetValue(context, port);
		}
	}

}
