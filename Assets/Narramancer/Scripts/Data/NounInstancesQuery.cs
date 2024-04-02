using System.Linq;

namespace Narramancer {
	public struct NounInstancesQuery {
		public PropertyScriptableObject[] mustHaveProperties;
		public PropertyScriptableObject[] mustNotHaveProperties;

		public override bool Equals(object other) {
			if (other is not NounInstancesQuery) {
				return false;
			}
			var otherQuery = (NounInstancesQuery) other;
			return mustHaveProperties.ContainsAll(otherQuery.mustHaveProperties) && otherQuery.mustHaveProperties.ContainsAll(mustHaveProperties) &&
				mustNotHaveProperties.ContainsAll(otherQuery.mustNotHaveProperties) && otherQuery.mustNotHaveProperties.ContainsAll(mustNotHaveProperties);
		}

		public override int GetHashCode() {
			int hash = 0;
			foreach( var property in mustHaveProperties) {
				hash ^= property.GetHashCode();
			}
			hash *= 7;
			foreach (var property in mustNotHaveProperties) {
				hash ^= property.GetHashCode();
			}
			return hash;
		}
	}
}
