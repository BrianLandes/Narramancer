using System;
using UnityEngine;

namespace Narramancer.Serialization {

	/// <summary>
	/// The production <see cref="IReferenceResolver"/>: saved references are keyed by the asset's GUID and
	/// local file id, both of which live in the project and survive a rebuild.
	/// <para>
	/// Three sources answer, in order: <see cref="RuntimeAssetRegistry"/> for assets a game registered by
	/// hand, <see cref="AssetIds"/> for anything with an asset identity, and the baked
	/// <see cref="AssetManifest"/> last. In the editor <c>AssetIds</c> reads the asset database directly, so
	/// it is always current; in a build only its <c>res:</c> form still works and everything else falls to the
	/// manifest.
	/// </para>
	/// <para>
	/// Replacing this with anything session-local reintroduces the bug it exists to fix — see
	/// docs/ODIN_REMOVAL_PLAN.md §2A, where a stale reference resumed a story at the wrong node with no
	/// error at all.
	/// </para>
	/// </summary>
	public sealed class UnityReferenceResolver : IReferenceResolver {

		private readonly AssetManifest manifest;

		/// <summary>Resolves against this project's baked manifest, plus the asset database in the editor.</summary>
		public UnityReferenceResolver() : this(AssetManifest.Load()) {
		}

		public UnityReferenceResolver(AssetManifest manifest) {
			this.manifest = manifest;
		}

		/// <summary>The manifest this resolver falls back to, or null if the project has none baked.</summary>
		public AssetManifest Manifest => manifest;

		// Order matters, and it is the same in both directions. The registry comes first because it is the
		// author saying explicitly "this asset, under this name", which should beat anything inferred.
		// AssetIds is next: in the editor it answers from the asset database, which is always current, and its
		// res: form answers from Resources anywhere at all. The manifest is last — it is the fallback that
		// carries a build, and the one thing here that can go stale.

		public bool TryGetId(UnityEngine.Object asset, out string id) {
			if (RuntimeAssetRegistry.TryGetId(asset, out id)) {
				return true;
			}
			if (AssetIds.TryGetId(asset, out id)) {
				return true;
			}
			return manifest != null && manifest.TryGetId(asset, out id);
		}

		public bool TryResolve(string id, Type type, out UnityEngine.Object asset) {
			if (RuntimeAssetRegistry.TryResolve(id, type, out asset)) {
				return true;
			}
			if (AssetIds.TryResolve(id, out asset) && (type == null || type.IsInstanceOfType(asset))) {
				return true;
			}
			if (manifest != null && manifest.TryResolve(id, type, out asset)) {
				return true;
			}
			asset = null;
			return false;
		}
	}
}
