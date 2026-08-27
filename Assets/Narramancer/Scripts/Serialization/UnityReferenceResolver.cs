using System;
using UnityEngine;

namespace Narramancer.Serialization {

	/// <summary>
	/// The production <see cref="IReferenceResolver"/>: saved references are keyed by the asset's GUID and
	/// local file id, both of which live in the project and survive a rebuild.
	/// <para>
	/// In the editor <see cref="AssetIds"/> answers directly from the asset database, which is always
	/// current. In a build there is no asset database, so everything goes through the
	/// <see cref="AssetManifest"/> baked before the build. The editor consults the manifest as a fallback,
	/// which matters mainly for ids the database has lost track of.
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

		public bool TryGetId(UnityEngine.Object asset, out string id) {
			if (AssetIds.TryGetId(asset, out id)) {
				return true;
			}
			return manifest != null && manifest.TryGetId(asset, out id);
		}

		public bool TryResolve(string id, Type type, out UnityEngine.Object asset) {
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
