using System;
using UnityEngine;

namespace Narramancer.Serialization {

	/// <summary>
	/// Turns a project asset into a <b>persistent</b> id and back again.
	/// <para>
	/// There are two forms, and which one an asset gets is decided by where it lives:
	/// </para>
	/// <list type="bullet">
	/// <item><description>
	/// <c>res:Path/Name</c> (or <c>res:Path/Name#SubAsset</c>) for anything under a <c>Resources</c> folder.
	/// This is the useful one for a game's own content, because <c>Resources.Load</c> works in a build with
	/// no bake and no bookkeeping — drop a sprite in <c>Resources</c> and saving it just works. The trade is
	/// that identity is the path, so <b>moving the asset out of Resources invalidates saved references to
	/// it</b> — the same contract <c>Resources.Load</c> already imposes on code that calls it.
	/// </description></item>
	/// <item><description>
	/// <c>&lt;asset GUID&gt;:&lt;local file id&gt;</c> for everything else — the identity Unity itself keeps,
	/// in the asset's <c>.meta</c> file. It survives the asset being moved or renamed, and resolving it in a
	/// build needs the <see cref="AssetManifest"/>.
	/// </description></item>
	/// </list>
	/// <para>
	/// The local file id is not decoration in either form. xNode nodes <em>are</em> assets, stored as
	/// sub-assets inside their graph, and a sprite in a sprite sheet is a sub-asset of its texture — so a
	/// saved reference routinely names one object inside a shared file. A path or GUID alone cannot tell two
	/// of them apart.
	/// </para>
	/// <para>
	/// This is the whole point of the exercise. The Odin path stored <c>instanceID</c>s, which come from a
	/// session-global counter handing out numbers in load order — the save recorded "the 46,810th object
	/// loaded this session" and nothing about the asset. Everything here is written into the project and
	/// changes only when the author changes it.
	/// </para>
	/// <para>
	/// Minting an id needs <c>AssetDatabase</c> and so only works in the editor. <em>Resolving</em> a
	/// <c>res:</c> id works anywhere, which is what makes that form worth having; a GUID id in a build goes
	/// through the manifest instead.
	/// </para>
	/// </summary>
	public static class AssetIds {

		/// <summary>Separates the asset GUID from the local file id within it.</summary>
		public const char Separator = ':';

		/// <summary>Marks an id as a path into a <c>Resources</c> folder.</summary>
		public const string ResourcePrefix = "res:";

		/// <summary>Separates a resource path from the name of a sub-asset inside it.</summary>
		public const char SubAssetSeparator = '#';

		private const string ResourcesFolder = "/Resources/";

		/// <summary>True where <see cref="TryGetId"/> can mint ids at all — the editor.</summary>
		public static bool CanMint =>
#if UNITY_EDITOR
			true;
#else
			false;
#endif

		/// <summary>
		/// The persistent id for an asset, or false if it is not one — an object created at runtime, or a
		/// scene object, has no asset identity and cannot be pointed at by a save.
		/// </summary>
		public static bool TryGetId(UnityEngine.Object asset, out string id) {
#if UNITY_EDITOR
			if (asset != null && UnityEditor.AssetDatabase.Contains(asset)) {
				var path = UnityEditor.AssetDatabase.GetAssetPath(asset);
				if (TryGetResourceId(asset, path, out id)) {
					return true;
				}
				if (UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var guid, out long localId)
					&& !guid.IsNullOrEmpty()) {
					id = $"{guid}{Separator}{localId}";
					return true;
				}
			}
#endif
			id = null;
			return false;
		}

		/// <summary>The asset an id names, or false if this project no longer has it.</summary>
		public static bool TryResolve(string id, out UnityEngine.Object asset) {
			// Checked first, and outside the editor guard, because this is the form a build can resolve on
			// its own.
			if (TryResolveResource(id, out asset)) {
				return true;
			}

#if UNITY_EDITOR
			if (TrySplit(id, out var guid, out var localId)) {
				var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
				if (!path.IsNullOrEmpty()) {
					foreach (var candidate in UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path)) {
						if (candidate != null
							&& UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(candidate, out _, out long candidateId)
							&& candidateId == localId) {
							asset = candidate;
							return true;
						}
					}
				}
			}
#endif
			asset = null;
			return false;
		}

		/// <summary>Splits a GUID-form id into its GUID and local file id. False for any other form.</summary>
		public static bool TrySplit(string id, out string guid, out long localId) {
			guid = null;
			localId = 0;
			if (id.IsNullOrEmpty() || IsResourceId(id)) {
				return false;
			}

			var separator = id.IndexOf(Separator);
			if (separator <= 0) {
				return false;
			}

			guid = id.Substring(0, separator);
			return long.TryParse(id.Substring(separator + 1), out localId);
		}

		public static bool IsResourceId(string id) =>
			id != null && id.StartsWith(ResourcePrefix, StringComparison.Ordinal);

#if UNITY_EDITOR
		private static bool TryGetResourceId(UnityEngine.Object asset, string path, out string id) {
			id = null;
			if (path.IsNullOrEmpty()) {
				return false;
			}

			// The last Resources folder on the path wins, matching how Resources.Load reads it.
			var folder = path.LastIndexOf(ResourcesFolder, StringComparison.Ordinal);
			if (folder < 0) {
				return false;
			}

			var relative = path.Substring(folder + ResourcesFolder.Length);
			var extension = relative.LastIndexOf('.');
			if (extension >= 0) {
				relative = relative.Substring(0, extension);
			}
			if (relative.IsNullOrEmpty()) {
				return false;
			}

			if (UnityEditor.AssetDatabase.IsMainAsset(asset)) {
				id = ResourcePrefix + relative;
				return true;
			}

			// A sub-asset is addressed by name within its file. A name carrying the separator would be
			// ambiguous on the way back, so those fall through to the GUID form rather than risk it.
			var name = asset.name;
			if (name.IsNullOrEmpty() || name.IndexOf(SubAssetSeparator) >= 0) {
				return false;
			}

			id = $"{ResourcePrefix}{relative}{SubAssetSeparator}{name}";
			return true;
		}
#endif

		private static bool TryResolveResource(string id, out UnityEngine.Object asset) {
			asset = null;
			if (!IsResourceId(id)) {
				return false;
			}

			var body = id.Substring(ResourcePrefix.Length);
			var separator = body.IndexOf(SubAssetSeparator);
			if (separator < 0) {
				asset = Resources.Load(body);
				return asset != null;
			}

			var path = body.Substring(0, separator);
			var name = body.Substring(separator + 1);

			// Resources.Load returns the main asset, so a sub-asset has to be picked out by name. Two
			// sub-assets sharing a name are left unresolved rather than guessed at — guessing is exactly the
			// failure mode this whole change exists to remove.
			UnityEngine.Object match = null;
			var matches = 0;
			foreach (var candidate in Resources.LoadAll(path)) {
				if (candidate != null && string.Equals(candidate.name, name, StringComparison.Ordinal)) {
					match = candidate;
					matches++;
				}
			}

			if (matches == 1) {
				asset = match;
				return true;
			}
			return false;
		}
	}
}
