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
			return SameProperties(mustHaveProperties, otherQuery.mustHaveProperties) &&
				SameProperties(mustNotHaveProperties, otherQuery.mustNotHaveProperties);
		}

		/// <summary>
		/// Set comparison that tolerates unset arrays. Either field can be left null by a query that doesn't
		/// constrain on it, which used to null-deref in <see cref="Equals"/>; null and empty both mean
		/// "no constraint", so they compare equal (and hash equal, keeping the two in agreement).
		/// </summary>
		private static bool SameProperties(PropertyScriptableObject[] a, PropertyScriptableObject[] b) {
			var aIsEmpty = a == null || a.Length == 0;
			var bIsEmpty = b == null || b.Length == 0;
			if (aIsEmpty || bIsEmpty) {
				return aIsEmpty && bIsEmpty;
			}
			return a.ContainsAll(b) && b.ContainsAll(a);
		}

		public override int GetHashCode() {
			int hash = 0;
			if (mustHaveProperties != null) {
				foreach (var property in mustHaveProperties) {
					if (property != null) {
						hash ^= property.GetHashCode();
					}
				}
			}
			hash *= 7;
			if (mustNotHaveProperties != null) {
				foreach (var property in mustNotHaveProperties) {
					if (property != null) {
						hash ^= property.GetHashCode();
					}
				}
			}
			return hash;
		}
	}
}
