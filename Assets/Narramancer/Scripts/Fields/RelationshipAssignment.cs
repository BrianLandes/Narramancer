
using System;

namespace Narramancer {

	[Serializable]
	public class RelationshipAssignment {
		public RelationshipScriptableObject relationship;
		public NounScriptableObject other;
		public SourceOrDestination sourceOrDestination;
	}
}