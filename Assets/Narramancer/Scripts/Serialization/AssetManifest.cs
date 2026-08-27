using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Narramancer.Serialization {

	/// <summary>
	/// The id ↔ asset table a <b>build</b> resolves saved references through, baked before the build by
	/// <see cref="AssetManifestBuilder"/>. <c>AssetDatabase</c> does not exist in a player, so without this
	/// a build has no way to turn the GUID in a save back into the asset it names.
	/// <para>
	/// It doubles as the list of assets a save is allowed to point at. Everything Narramancer authors —
	/// nouns, adjectives, flags, verb graphs and the nodes inside them — is collected automatically;
	/// <see cref="AdditionalAssets"/> is for anything else a game puts in a blackboard, a sprite or a clip,
	/// which nothing can discover by scanning for Narramancer types. An asset in neither place fails at
	/// <em>save</em> time with a message naming it, which is a problem the author can see and fix, rather
	/// than at load time in a shipped build, which is not.
	/// </para>
	/// </summary>
	public class AssetManifest : ScriptableObject {

		[Serializable]
		public class Entry {
			public string id;
			public UnityEngine.Object asset;
		}

		[SerializeField]
		private List<Entry> entries = new List<Entry>();

		/// <summary>
		/// Assets to include that scanning cannot find: anything outside Narramancer's own types that a game
		/// stores in a blackboard and expects to survive a save. Authored, and preserved across a rebuild.
		/// </summary>
		[SerializeField]
		private List<UnityEngine.Object> additionalAssets = new List<UnityEngine.Object>();

		public IReadOnlyList<Entry> Entries => entries;

		public List<UnityEngine.Object> AdditionalAssets => additionalAssets;

		public int Count => entries.Count;

		private Dictionary<string, UnityEngine.Object> byId;
		private Dictionary<UnityEngine.Object, string> idByAsset;

		/// <summary>The manifest in this project's Resources, or null if none has been baked yet.</summary>
		public static AssetManifest Load() {
			if (loaded == null) {
				loaded = Resources.LoadAll<AssetManifest>(string.Empty).FirstOrDefault();
			}
			return loaded;
		}
		private static AssetManifest loaded;

		/// <summary>Replaces the whole table. Called by the builder; the lookups rebuild on next use.</summary>
		public void SetEntries(IEnumerable<Entry> newEntries) {
			entries = newEntries.ToList();
			Invalidate();
		}

		public void Invalidate() {
			byId = null;
			idByAsset = null;
		}

		public bool TryGetId(UnityEngine.Object asset, out string id) {
			if (asset == null) {
				id = null;
				return false;
			}
			EnsureLookups();
			return idByAsset.TryGetValue(asset, out id);
		}

		public bool TryResolve(string id, Type type, out UnityEngine.Object asset) {
			if (!id.IsNullOrEmpty()) {
				EnsureLookups();
				if (byId.TryGetValue(id, out asset) && asset != null && (type == null || type.IsInstanceOfType(asset))) {
					return true;
				}
			}
			asset = null;
			return false;
		}

		private void EnsureLookups() {
			if (byId != null) {
				return;
			}

			byId = new Dictionary<string, UnityEngine.Object>(StringComparer.Ordinal);
			idByAsset = new Dictionary<UnityEngine.Object, string>();
			foreach (var entry in entries) {
				if (entry == null || entry.id.IsNullOrEmpty() || entry.asset == null) {
					continue;
				}
				byId[entry.id] = entry.asset;
				idByAsset[entry.asset] = entry.id;
			}
		}
	}
}
