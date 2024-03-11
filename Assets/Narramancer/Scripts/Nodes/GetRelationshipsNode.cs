using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("Adjective/Get Relationships")]
	public class GetRelationshipsNode : AbstractInstanceInputNode {

		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField, HideLabel]
		public RelationshipScriptableObject relationship = default;

		[SerializeField]
		RelationshipRequirement relationshipRequirement = RelationshipRequirement.Either;

		[Output(ShowBackingValue.Never)]
		[SerializeField]
		private bool anyRelationships = false;

		[Output(ShowBackingValue.Never)]
		[SerializeField]
		private List<RelationshipInstance> relaionshipInstances = default;

		[Output(ShowBackingValue.Never)]
		[SerializeField]
		private List<NounInstance> otherInstances = default;

		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying) {

				var inputInstance = GetInstance(context);
				if (inputInstance == null) {
					Debug.LogError("Instance was null", this);
					return null;
				}

				var relationship = GetInputValue(context, nameof(this.relationship), this.relationship);
				if (relationship == null) {
					Debug.LogError("Relationship was null", this);
					return null;
				}

				var allRelationships = inputInstance.GetRelationshipsWith(relationship);
				switch (relationshipRequirement) {
					case RelationshipRequirement.Source:
						allRelationships = allRelationships.Where(relationshipInstance => relationshipInstance.InvolvesSource(inputInstance)).ToList();
						break;
					case RelationshipRequirement.Destination:
						allRelationships = allRelationships.Where(relationshipInstance => relationshipInstance.InvolvesDestination(inputInstance)).ToList();
						break;
					case RelationshipRequirement.Either:
						// leave list as is
						break;
				}

				switch (port.fieldName) {
					case nameof(anyRelationships):
						return allRelationships.Any();
					case nameof(relaionshipInstances):
						return allRelationships;
					case nameof(otherInstances):
						return allRelationships.Select(relationshipInstance => relationshipInstance.GetOther(inputInstance)).ToList();
				}

			}
			return base.GetValue(context, port);
		}
	}

}
