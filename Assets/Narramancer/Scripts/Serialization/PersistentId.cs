using UnityEngine;

namespace Narramancer.Serialization {

	/// <summary>
	/// The one place that answers "what is this asset's durable name?" for code outside the serializer —
	/// blackboard keys, mainly. Backed by a shared <see cref="UnityReferenceResolver"/>, so it gives the same
	/// answer a save does: an asset GUID, a <c>Resources</c> path, or a runtime registry key.
	/// <para>
	/// It exists because <c>UnityEngine.Object.GetHashCode()</c> <b>is the instanceID</b> — verified, not
	/// assumed — and an instanceID is handed out by a session-global counter in load order. Anything keyed on
	/// one silently means something different after a patch. That is the failure documented in
	/// docs/ODIN_REMOVAL_PLAN.md §2A, and it was hiding in blackboard keys as well as in saved references.
	/// </para>
	/// </summary>
	public static class PersistentId {

		private static UnityReferenceResolver resolver = null;

		/// <summary>The shared resolver. Saves go through this too, so both agree on what an asset is called.</summary>
		public static UnityReferenceResolver Resolver {
			get {
				if (resolver == null) {
					resolver = new UnityReferenceResolver();
				}
				return resolver;
			}
		}

		/// <summary>Drops the cached resolver, so the next lookup picks up a freshly baked manifest.</summary>
		public static void Reset() {
			resolver = null;
		}

		/// <summary>
		/// The durable name for an asset, or <c>null</c> when it has none — an object created at runtime and
		/// never registered. Callers decide what to do about that; this does not guess.
		/// </summary>
		public static string Of(UnityEngine.Object asset) {
			return asset != null && Resolver.TryGetId(asset, out var id) ? id : null;
		}
	}
}
