using System;
using UnityEngine;

namespace Narramancer {

	[Serializable]
	public class RelationshipInstance : AdjectiveInstance<RelationshipScriptableObject> {

		[SerializeField]
		protected NounUID source;
		public NounUID SourceUID => source;

		[SerializeField]
		protected NounUID destination;
		public NounUID DestinationUID => destination;

		public RelationshipInstance(RelationshipScriptableObject adjective, NounInstance left, NounInstance right, SourceOrDestination leftIsSourceOrDestination = SourceOrDestination.Source) : base(adjective) {
			switch (leftIsSourceOrDestination) {
				case SourceOrDestination.Source:
					this.source = left.UID;
					this.destination = right.UID;
					break;
				case SourceOrDestination.Destination:
					this.source = right.UID;
					this.destination = left.UID;
					break;
			}
		}

		public bool Involves(RelationshipScriptableObject relationship, NounInstance source, NounInstance destination) {
			return relationship == this.Adjective && source.UID == this.source && destination.UID == this.destination;
		}

		public bool Involves(RelationshipScriptableObject relationship) {
			return relationship == this.Adjective;
		}

		public bool Involves(NounInstance instance, RelationshipRequirement requirement = RelationshipRequirement.Either) {
			switch (requirement) {
				case RelationshipRequirement.Source:
					return InvolvesSource(instance);
				case RelationshipRequirement.Destination:
					return InvolvesDestination(instance);
				default:
				case RelationshipRequirement.Either:
					return InvolvesSource(instance) || InvolvesDestination(instance);
			}
		}

		public bool InvolvesSource(NounInstance source) {
			return source.UID == this.source;
		}

		public bool InvolvesDestination(NounInstance destination) {
			return destination.UID == this.destination;
		}

		public bool Involves(NounInstance instance, RelationshipScriptableObject noun) {
			return noun == this.Adjective && (InvolvesSource(instance) || InvolvesDestination(instance));
		}

		public bool InvolvesSource(NounInstance source, RelationshipScriptableObject relationship) {
			return relationship == this.Adjective && source.UID == this.source;
		}

		public bool InvolvesDestination(NounInstance destination, RelationshipScriptableObject relationship) {
			return relationship == this.Adjective && destination.UID == this.destination;
		}

		public NounInstance GetSource() {
			return NarramancerSingleton.Instance.GetInstance(SourceUID);
		}

		public NounInstance GetDestination() {
			return NarramancerSingleton.Instance.GetInstance(DestinationUID);
		}

		public NounInstance GetOther(NounInstance instance) {
			if (instance.UID == SourceUID) {
				return GetDestination();
			}
			return GetSource();
		}
	}


	public enum RelationshipRequirement {
		Source,
		Destination,
		Either
	}

	public enum SourceOrDestination {
		Source,
		Destination
	}

	public static class RelationshipEnumExtensions {
		public static RelationshipRequirement AsRequirement(this SourceOrDestination sourceOrDestination) {
			return (RelationshipRequirement)((int)sourceOrDestination);
		}
	}
}