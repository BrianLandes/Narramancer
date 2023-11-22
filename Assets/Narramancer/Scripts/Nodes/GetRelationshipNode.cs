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


		public override object GetValue(object context, NodePort port) {
			if (Application.isPlaying) {
				switch (port.fieldName) {
					case nameof(hasRelationship): {
							var inputInstance = GetInstance(context);

							var relationship = GetInputValue(context, nameof(this.relationship), this.relationship);
							var other = GetInputValue(context, nameof(this.other), this.other);

							if (inputInstance != null && relationship != null && other != null) {
								return inputInstance.HasRelationship(relationship, other, relationshipRequirement);
							}
							else {
								Debug.LogError("Inputs were null", this);
								return null;
							}
						}

					case nameof(relaionshipInstance): {
							var inputInstance = GetInstance(context);

							var relationship = GetInputValue(context, nameof(this.relationship), this.relationship);
							var other = GetInputValue(context, nameof(this.other), this.other);

							if (inputInstance != null && relationship != null && other != null) {
								return inputInstance.GetRelationship(relationship, other, relationshipRequirement);
							}
							else {
								Debug.LogError("Inputs were null", this);
								return null;
							}
						}
				}

			}
			return base.GetValue(context, port);
		}
	}

}
