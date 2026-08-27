using System;
using System.Collections.Generic;

namespace Narramancer.Serialization {

	/// <summary>
	/// An <see cref="IReferenceResolver"/> backed by an explicit id ↔ object registry. Used by tests and
	/// by any caller that wants to pin references by hand rather than go through the asset database.
	/// Objects are keyed by <b>reference</b>, so two equal-by-value assets still get distinct ids.
	/// </summary>
	public sealed class InMemoryReferenceResolver : IReferenceResolver {

		private readonly Dictionary<string, UnityEngine.Object> assetById = new Dictionary<string, UnityEngine.Object>(StringComparer.Ordinal);
		private readonly Dictionary<object, string> idByAsset = new Dictionary<object, string>(ReferenceEqualityComparer.Instance);

		/// <summary>Registers an id ↔ asset pair. Re-registering an id replaces its mapping.</summary>
		public InMemoryReferenceResolver Register(string id, UnityEngine.Object asset) {
			if (id.IsNullOrEmpty()) {
				throw new ArgumentException("Reference id must not be null or empty.", nameof(id));
			}
			if (asset == null) {
				throw new ArgumentNullException(nameof(asset));
			}
			assetById[id] = asset;
			idByAsset[asset] = id;
			return this;
		}

		public bool TryResolve(string id, Type type, out UnityEngine.Object asset) {
			if (!id.IsNullOrEmpty() && assetById.TryGetValue(id, out asset) && (type == null || type.IsInstanceOfType(asset))) {
				return true;
			}
			asset = null;
			return false;
		}

		public bool TryGetId(UnityEngine.Object asset, out string id) {
			if (asset != null && idByAsset.TryGetValue(asset, out id)) {
				return true;
			}
			id = null;
			return false;
		}
	}
}
