using System;
using System.Collections.Generic;
using UnityEngine;

namespace Narramancer.Serialization {

	/// <summary>
	/// Assets a game registers by hand, under a key it chooses. The escape hatch for the one case nothing
	/// else can cover: an object that reaches a blackboard from game code rather than from a node or a
	/// Resources folder.
	/// <code>
	/// RuntimeAssetRegistry.Register("icon-sword", swordIcon);
	/// story.Blackboard.Set("equipped-icon", swordIcon);
	/// </code>
	/// <para>
	/// The key is the author's to keep stable — it is stored in the save exactly as given, so renaming one
	/// breaks references in existing saves the same way renaming a blackboard variable would. Register before
	/// loading, not just before saving: a load resolves through here too.
	/// </para>
	/// <para>
	/// Registrations are process-wide and are not saved. They are the last resort, deliberately: an asset
	/// under a <c>Resources</c> folder needs no registration at all, and Narramancer's own content is baked
	/// into the <see cref="AssetManifest"/> automatically.
	/// </para>
	/// </summary>
	public static class RuntimeAssetRegistry {

		/// <summary>Marks an id as a registry key rather than an asset GUID or a resource path.</summary>
		public const string Prefix = "ref:";

		private static readonly Dictionary<string, UnityEngine.Object> assetByKey = new Dictionary<string, UnityEngine.Object>(StringComparer.Ordinal);
		private static readonly Dictionary<UnityEngine.Object, string> keyByAsset = new Dictionary<UnityEngine.Object, string>();

		/// <summary>How many assets are currently registered.</summary>
		public static int Count => assetByKey.Count;

		/// <summary>
		/// Registers an asset under a key. Re-registering a key replaces what it points at, which is how a
		/// game can swap an asset out between sessions without invalidating saves that name it.
		/// </summary>
		public static void Register(string key, UnityEngine.Object asset) {
			if (key.IsNullOrEmpty()) {
				throw new ArgumentException("A registry key must not be null or empty.", nameof(key));
			}
			if (asset == null) {
				throw new ArgumentNullException(nameof(asset));
			}

			if (assetByKey.TryGetValue(key, out var previous) && previous != null) {
				keyByAsset.Remove(previous);
			}
			assetByKey[key] = asset;
			keyByAsset[asset] = key;
		}

		public static bool Unregister(string key) {
			if (!key.IsNullOrEmpty() && assetByKey.TryGetValue(key, out var asset)) {
				assetByKey.Remove(key);
				if (asset != null) {
					keyByAsset.Remove(asset);
				}
				return true;
			}
			return false;
		}

		public static void Clear() {
			assetByKey.Clear();
			keyByAsset.Clear();
		}

		public static bool TryGetId(UnityEngine.Object asset, out string id) {
			if (asset != null && keyByAsset.TryGetValue(asset, out var key)) {
				id = Prefix + key;
				return true;
			}
			id = null;
			return false;
		}

		public static bool TryResolve(string id, Type type, out UnityEngine.Object asset) {
			if (id != null && id.StartsWith(Prefix, StringComparison.Ordinal)
				&& assetByKey.TryGetValue(id.Substring(Prefix.Length), out asset)
				&& asset != null
				&& (type == null || type.IsInstanceOfType(asset))) {
				return true;
			}
			asset = null;
			return false;
		}
	}
}
