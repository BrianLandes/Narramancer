#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Narramancer.Serialization {

	/// <summary>
	/// Bakes the <see cref="AssetManifest"/> a build resolves saved references through. Runs automatically
	/// before every build, and can be run by hand from <c>Window/Narramancer/Rebuild Asset Manifest</c>.
	/// <para>
	/// What it collects is everything a save can point at: any asset whose type comes from Narramancer's own
	/// assembly — nouns, adjectives, flags, verb graphs and, importantly, the nodes stored as sub-assets
	/// inside those graphs — plus every asset that content references, everything under <c>Resources</c>, and
	/// whatever the author listed in <see cref="AssetManifest.AdditionalAssets"/>. A sprite or clip that only
	/// game code or a scene references has to be added there; nothing can discover those by scanning.
	/// </para>
	/// </summary>
	public static class AssetManifestBuilder {

		private const string DefaultDirectory = "Assets/Resources";
		private const string DefaultFileName = "NarramancerAssetManifest.asset";

		/// <summary>
		/// The asset types a save can point at, as roots — an <c>AssetDatabase</c> <c>t:</c> search matches
		/// subclasses, so a game's own noun or node types are covered without naming them. Anything outside these
		/// goes in <see cref="AssetManifest.AdditionalAssets"/>.
		/// </summary>
		private static readonly string[] RootTypes = {
			nameof(XNode.NodeGraph),
			nameof(NounScriptableObject),
			nameof(AdjectiveScriptableObject),
			nameof(Flag),
		};

		[MenuItem("Window/Narramancer/Rebuild Asset Manifest")]
		public static void RebuildFromMenu() {
			var manifest = Rebuild();
			Debug.Log($"Narramancer asset manifest rebuilt: {manifest.Count} assets.", manifest);
		}

		/// <summary>Rescans the project and writes the result into the manifest asset, creating it if needed.</summary>
		public static AssetManifest Rebuild() {
			var manifest = FindOrCreate();
			Populate(manifest);
			EditorUtility.SetDirty(manifest);
			AssetDatabase.SaveAssets();
			return manifest;
		}

		/// <summary>Fills a manifest from the current project without touching the asset database.</summary>
		public static void Populate(AssetManifest manifest) {
			var entries = new List<AssetManifest.Entry>();
			var seen = new HashSet<string>();

			foreach (var asset in Collect(manifest)) {
				if (AssetIds.TryGetId(asset, out var id) && seen.Add(id)) {
					entries.Add(new AssetManifest.Entry { id = id, asset = asset });
				}
			}

			// Sorted by id, not left in discovery order. Sorting the *paths* is not enough: the order of
			// sub-assets within one file comes from LoadAllAssetsAtPath and is not stable between imports, so a
			// graph's nodes would shuffle and the baked asset would churn in version control on every rebake for
			// no actual change.
			entries.Sort((left, right) => string.CompareOrdinal(left.id, right.id));
			manifest.SetEntries(entries);
		}

		/// <summary>Every asset a save is allowed to point at, in a stable order.</summary>
		public static IEnumerable<UnityEngine.Object> Collect(AssetManifest manifest) {
			var collected = new List<UnityEngine.Object>();

			// Searching by root type rather than by ScriptableObject: `t:` matches subclasses, so these four
			// cover everything Narramancer authors while leaving a project's unrelated assets unopened. The
			// difference is not cosmetic — the broad version loads every ScriptableObject in the project, which
			// on a real game is minutes of work for assets a save can never point at.
			// Sorting by path keeps the baked manifest stable between rebuilds, so it does not churn in version
			// control every time an unrelated asset is added.
			var paths = RootTypes
				.SelectMany(rootType => AssetDatabase.FindAssets($"t:{rootType}"))
				.Select(AssetDatabase.GUIDToAssetPath)
				.Where(path => !path.IsNullOrEmpty())
				.Distinct()
				.OrderBy(path => path, System.StringComparer.Ordinal);

			var narramancerAssets = new List<UnityEngine.Object>();
			foreach (var path in paths) {
				// LoadAllAssetsAtPath rather than LoadAssetAtPath: the nodes of a verb graph are sub-assets
				// of it, and they are exactly what a suspended runner points at.
				foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path)) {
					if (asset != null && IsNarramancerAsset(asset)) {
						narramancerAssets.Add(asset);
					}
				}
			}
			collected.AddRange(narramancerAssets);

			// Whatever that content points at: the sprite in an equipment noun's blackboard, the clip a node
			// plays. A verb hands these to scene components, so a save has to be able to name them — and they
			// cost nothing in build size, since they ship because the content referencing them does. Without
			// this, a sprite a verb put on an Image saved fine in the editor and silently fell back to the scene's
			// sprite in a build.
			foreach (var dependency in EditorUtility.CollectDependencies(narramancerAssets.ToArray())) {
				if (IsNameableDependency(dependency)) {
					collected.Add(dependency);
				}
			}

			// Everything under a Resources folder, so a save can name a game's own sprites and clips with no
			// setup at all. This costs nothing in build size — Resources content ships wholesale either way — and
			// it is what lets a *build* mint a res: id, which needs the reverse lookup these entries provide.
			foreach (var path in ResourceAssetPaths()) {
				foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path)) {
					if (asset == null) {
						continue;
					}
					// A prefab reports every GameObject and Component inside it. Only its root can be named by a
					// resource path, and the rest would bloat the manifest by orders of magnitude.
					if (!AssetDatabase.IsMainAsset(asset) && (asset is GameObject || asset is Component)) {
						continue;
					}
					// A table listing itself is noise, and neither singleton is something a save points at.
					if (asset is AssetManifest || asset is SingletonBase) {
						continue;
					}
					collected.Add(asset);
				}
			}

			if (manifest != null) {
				collected.AddRange(manifest.AdditionalAssets.Where(asset => asset != null));
			}

			return collected;
		}

		/// <summary>
		/// Every asset file sitting under a <c>Resources</c> folder in this project.
		/// </summary>
		private static IEnumerable<string> ResourceAssetPaths() {
			// Located on the filesystem and then searched folder by folder, rather than by filtering
			// AssetDatabase.GetAllAssetPaths(): that call enumerates every path in the project *and* its
			// packages, which is tens of thousands of strings to find a handful of folders.
			var assets = Application.dataPath.Replace('\\', '/');
			var folders = Directory.GetDirectories(assets, "Resources", SearchOption.AllDirectories)
				.Select(folder => "Assets" + folder.Replace('\\', '/').Substring(assets.Length))
				// A Resources folder inside an Editor folder is editor-only and never ships. Including one would
				// do the opposite of the intent here: the manifest is a runtime asset, so referencing xNode's
				// editor GUI textures from it would drag them into every build.
				.Where(folder => folder.IndexOf("/Editor/", System.StringComparison.Ordinal) < 0)
				.ToArray();
			if (folders.Length == 0) {
				return System.Array.Empty<string>();
			}

			return AssetDatabase.FindAssets(string.Empty, folders)
				.Select(AssetDatabase.GUIDToAssetPath)
				.Where(path => !path.IsNullOrEmpty() && !AssetDatabase.IsValidFolder(path))
				.Distinct()
				.OrderBy(path => path, System.StringComparer.Ordinal);
		}

		/// <summary>
		/// True when the asset's type, or one it derives from, is declared in Narramancer's own assembly.
		/// Walking the base chain is what includes a game's own node and noun subclasses, which live in the
		/// game's assembly but are still Narramancer content.
		/// </summary>
		private static bool IsNarramancerAsset(UnityEngine.Object asset) {
			var narramancer = typeof(NarramancerSingleton).Assembly;
			for (var type = asset.GetType(); type != null && type != typeof(ScriptableObject); type = type.BaseType) {
				if (type.Assembly == narramancer) {
					return true;
				}
			}
			return false;
		}

		/// <summary>
		/// True for a dependency a save could hold: a project asset, but not a script, not the inside of a prefab,
		/// and not the manifest or a singleton.
		/// </summary>
		private static bool IsNameableDependency(UnityEngine.Object asset) {
			if (asset == null || !AssetDatabase.Contains(asset) || asset is MonoScript) {
				return false;
			}
			if (!AssetDatabase.IsMainAsset(asset) && (asset is GameObject || asset is Component)) {
				return false;
			}
			return !(asset is AssetManifest || asset is SingletonBase);
		}

		private static AssetManifest FindOrCreate() {
			var existing = AssetDatabase.FindAssets($"t:{nameof(AssetManifest)}")
				.Select(AssetDatabase.GUIDToAssetPath)
				.Select(AssetDatabase.LoadAssetAtPath<AssetManifest>)
				.FirstOrDefault(manifest => manifest != null);
			if (existing != null) {
				return existing;
			}

			// Alongside the singleton if the project already has one, since that is a Resources folder the
			// project is known to load from.
			var singleton = Resources.LoadAll<NarramancerSingleton>(string.Empty).FirstOrDefault();
			var directory = DefaultDirectory;
			if (singleton != null) {
				var singletonPath = AssetDatabase.GetAssetPath(singleton);
				if (!singletonPath.IsNullOrEmpty()) {
					directory = Path.GetDirectoryName(singletonPath).Replace('\\', '/');
				}
			}

			Directory.CreateDirectory(directory);
			var created = ScriptableObject.CreateInstance<AssetManifest>();
			AssetDatabase.CreateAsset(created, $"{directory}/{DefaultFileName}");
			return created;
		}

		/// <summary>Rebuilds the manifest before a build, so a player has something to resolve against.</summary>
		public class BuildPreprocessor : IPreprocessBuildWithReport {

			public int callbackOrder => 0;

			public void OnPreprocessBuild(BuildReport report) {
				Rebuild();
			}
		}
	}
}
#endif
