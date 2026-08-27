using UnityEngine;

namespace Narramancer.Serialization {

	/// <summary>
	/// Turns a project asset into a <b>persistent</b> id and back again, using the identity Unity itself
	/// stores on disk: the GUID from the asset's <c>.meta</c> file, plus the local file id that picks a
	/// sub-asset out of it.
	/// <para>
	/// The local file id is not optional decoration. xNode nodes <em>are</em> assets, stored as sub-assets
	/// inside their graph, so a saved reference to "the node this verb is suspended on" is a reference to a
	/// sub-asset. A GUID alone names the graph and cannot tell two of its nodes apart.
	/// </para>
	/// <para>
	/// This is the whole point of the exercise. The Odin path stored <c>instanceID</c>s, which come from a
	/// session-global counter handing out numbers in load order — the save recorded "the 46,810th object
	/// loaded this session" and nothing about the asset. Both halves of the id here are written into the
	/// project and change only when the author changes them.
	/// </para>
	/// <para>
	/// Only usable in the editor, because <c>AssetDatabase</c> does not exist in a build. A build resolves
	/// through <see cref="AssetManifest"/> instead, which is baked from exactly these ids.
	/// </para>
	/// </summary>
	public static class AssetIds {

		/// <summary>Separates the asset GUID from the local file id within it.</summary>
		public const char Separator = ':';

		/// <summary>True where <see cref="TryGetId"/> and <see cref="TryResolve"/> can do any work at all.</summary>
		public static bool Available =>
#if UNITY_EDITOR
			true;
#else
			false;
#endif

		/// <summary>
		/// The persistent id for an asset, or false if it is not one — an object created at runtime, or a
		/// scene object, has no asset identity and cannot be pointed at by a save.
		/// </summary>
		public static bool TryGetId(Object asset, out string id) {
#if UNITY_EDITOR
			if (asset != null
				&& UnityEditor.AssetDatabase.Contains(asset)
				&& UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset, out var guid, out long localId)
				&& !guid.IsNullOrEmpty()) {
				id = $"{guid}{Separator}{localId}";
				return true;
			}
#endif
			id = null;
			return false;
		}

		/// <summary>The asset an id names, or false if this project no longer has it.</summary>
		public static bool TryResolve(string id, out Object asset) {
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

		/// <summary>Splits an id back into its GUID and local file id.</summary>
		public static bool TrySplit(string id, out string guid, out long localId) {
			guid = null;
			localId = 0;
			if (id.IsNullOrEmpty()) {
				return false;
			}

			var separator = id.IndexOf(Separator);
			if (separator <= 0) {
				return false;
			}

			guid = id.Substring(0, separator);
			return long.TryParse(id.Substring(separator + 1), out localId);
		}
	}
}
