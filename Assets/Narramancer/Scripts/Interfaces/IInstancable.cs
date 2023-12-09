
using System.Collections.Generic;
namespace Narramancer {
	public interface IInstancable {
		string DisplayName { get; }
		NounUID ID { get; }
		Pronouns Pronouns { get; }
		IEnumerable<PropertyAssignment> Properties { get; }
		IEnumerable<StatAssignment> Stats { get; }
		IEnumerable<RelationshipAssignment> Relationships { get; }
		Blackboard Blackboard { get; }
	}
}