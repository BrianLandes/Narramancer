using System.Collections.Generic;
using System.IO;
using System.Linq;
using Narramancer;
using Narramancer.Serialization;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Asset references keyed by GUID rather than <c>instanceID</c> — the correctness fix at the centre of
/// docs/ODIN_REMOVAL_PLAN.md §2A.
/// <para>
/// The bug being fixed: an <c>instanceID</c> is handed out by a session-global counter in load order, so a
/// save recorded "the 46,810th object loaded this session" and nothing about the asset. Shipping a patch
/// renumbers everything. It was reproduced on 2026-08-27 and every reference had shifted by exactly 44 —
/// with <b>no errors, no nulls</b>, just a story that resumed at the wrong node.
/// </para>
/// <para>
/// These tests write real assets to disk, because that is the only way to exercise identity that is supposed
/// to be persistent. An in-memory <c>ScriptableObject</c> has no asset identity at all, which is itself one
/// of the behaviours asserted here.
/// </para>
/// </summary>
public class AssetReferenceTests {

	private const string TestDirectory = "Assets/Temp Asset Reference Tests";

	[SetUp]
	public void SetUp() {
		RecordingTestNode.Log.Clear();
		if (!AssetDatabase.IsValidFolder(TestDirectory)) {
			AssetDatabase.CreateFolder("Assets", Path.GetFileName(TestDirectory));
		}
	}

	[TearDown]
	public void TearDown() {
		if (AssetDatabase.IsValidFolder(TestDirectory)) {
			AssetDatabase.DeleteAsset(TestDirectory);
		}
	}

	/// <summary>Writes a real asset to disk, so it has a GUID and a .meta file.</summary>
	private T CreateAsset<T>(string name) where T : ScriptableObject {
		var asset = ScriptableObject.CreateInstance<T>();
		asset.name = name;
		AssetDatabase.CreateAsset(asset, $"{TestDirectory}/{name}.asset");
		return asset;
	}

	private static void PumpUntilIdle(NodeRunner runner, int maxSteps = 32) {
		var steps = 0;
		while (steps < maxSteps && runner.Update()) {
			steps++;
		}
		Assert.Less(steps, maxSteps, "runner did not settle");
	}

	/// <summary>Adds a node inside an existing asset, the way xNode stores a graph's nodes.</summary>
	private T AddSubAsset<T>(Object parent, string name) where T : ScriptableObject {
		var asset = ScriptableObject.CreateInstance<T>();
		asset.name = name;
		AssetDatabase.AddObjectToAsset(asset, parent);
		AssetDatabase.SaveAssets();
		return asset;
	}

	#region The id is the one Unity persists

	[Test]
	public void AnAssetIdIsBuiltFromTheGuidInItsMetaFile() {
		// The GUID is written into the .meta file next to the asset and is what Unity itself uses to
		// reference it. Reading it off disk is the strongest available proof that the id is persistent
		// rather than session-local.
		var noun = CreateAsset<NounScriptableObject>("Wren");
		var path = AssetDatabase.GetAssetPath(noun);

		Assert.IsTrue(AssetIds.TryGetId(noun, out var id));
		Assert.IsTrue(AssetIds.TrySplit(id, out var guid, out _));

		var metaText = File.ReadAllText($"{path}.meta");
		StringAssert.Contains($"guid: {guid}", metaText, "the id must be the identity Unity wrote to disk, not one invented at runtime");
	}

	[Test]
	public void AnAssetIdIsNotTheInstanceId() {
		// Belt and braces against a regression back to the old scheme. instanceIDs shifted by 44 between two
		// editor sessions in the experiment that proved the bug.
		var noun = CreateAsset<NounScriptableObject>("Wren");

		Assert.IsTrue(AssetIds.TryGetId(noun, out var id));

		StringAssert.DoesNotContain(noun.GetInstanceID().ToString(), id);
	}

	[Test]
	public void AnAssetIdResolvesBackToTheSameAsset() {
		var noun = CreateAsset<NounScriptableObject>("Wren");
		AssetIds.TryGetId(noun, out var id);

		Assert.IsTrue(AssetIds.TryResolve(id, out var resolved));

		Assert.AreSame(noun, resolved);
	}

	[Test]
	public void ObjectsThatAreNotAssetsHaveNoId() {
		// A runtime-created object cannot be pointed at by a save, and saying so here is what makes the
		// writer's refusal to serialize one a deliberate behaviour rather than an accident.
		var loose = ScriptableObject.CreateInstance<NounScriptableObject>();
		try {
			Assert.IsFalse(AssetIds.TryGetId(loose, out _));
		}
		finally {
			Object.DestroyImmediate(loose);
		}
	}

	[Test]
	public void AnIdForAnAssetThisProjectNoLongerHasDoesNotResolve() {
		Assert.IsFalse(AssetIds.TryResolve("00000000000000000000000000000000:11400000", out _));
	}

	#endregion

	#region Sub-assets — the xNode case

	/// <summary>
	/// xNode nodes are sub-assets of their graph, so "the node this verb is suspended on" is a reference to
	/// one object inside a shared asset file. A GUID alone names the graph and cannot tell two nodes apart —
	/// which is why the id carries the local file id too.
	/// </summary>
	[Test]
	public void NodesInsideAGraphGetDistinctIdsThatResolveIndependently() {
		var graph = CreateAsset<ActionVerb>("Verb");
		var first = AddSubAsset<PrintTextNode>(graph, "first");
		var second = AddSubAsset<PrintTextNode>(graph, "second");

		Assert.IsTrue(AssetIds.TryGetId(first, out var firstId));
		Assert.IsTrue(AssetIds.TryGetId(second, out var secondId));

		Assert.AreNotEqual(firstId, secondId, "two nodes in one graph must not share an id");
		AssetIds.TrySplit(firstId, out var firstGuid, out _);
		AssetIds.TrySplit(secondId, out var secondGuid, out _);
		Assert.AreEqual(firstGuid, secondGuid, "precondition: they really are sub-assets of the same file");

		Assert.IsTrue(AssetIds.TryResolve(firstId, out var resolvedFirst));
		Assert.IsTrue(AssetIds.TryResolve(secondId, out var resolvedSecond));
		Assert.AreSame(first, resolvedFirst);
		Assert.AreSame(second, resolvedSecond);
	}

	#endregion

	#region The manifest — how a build resolves

	[Test]
	public void TheManifestResolvesInBothDirections() {
		var noun = CreateAsset<NounScriptableObject>("Wren");
		AssetIds.TryGetId(noun, out var id);
		var manifest = ScriptableObject.CreateInstance<AssetManifest>();
		manifest.SetEntries(new[] { new AssetManifest.Entry { id = id, asset = noun } });

		try {
			Assert.IsTrue(manifest.TryGetId(noun, out var roundTripped));
			Assert.AreEqual(id, roundTripped);
			Assert.IsTrue(manifest.TryResolve(id, typeof(NounScriptableObject), out var resolved));
			Assert.AreSame(noun, resolved);
		}
		finally {
			Object.DestroyImmediate(manifest);
		}
	}

	[Test]
	public void TheManifestRefusesAnAssetOfTheWrongType() {
		// A saved reference declares what it expects to find. Returning the wrong type would be the same
		// class of silent corruption as the instanceID bug.
		var noun = CreateAsset<NounScriptableObject>("Wren");
		AssetIds.TryGetId(noun, out var id);
		var manifest = ScriptableObject.CreateInstance<AssetManifest>();
		manifest.SetEntries(new[] { new AssetManifest.Entry { id = id, asset = noun } });

		try {
			Assert.IsFalse(manifest.TryResolve(id, typeof(ActionVerb), out _));
		}
		finally {
			Object.DestroyImmediate(manifest);
		}
	}

	[Test]
	public void TheManifestKnowsNothingItWasNotGiven() {
		// This is the build path: with no asset database, an asset the bake missed is simply unresolvable.
		// It is why the manifest doubles as the list of what a save is allowed to point at.
		var manifest = ScriptableObject.CreateInstance<AssetManifest>();

		try {
			Assert.IsFalse(manifest.TryResolve("00000000000000000000000000000000:11400000", null, out _));
		}
		finally {
			Object.DestroyImmediate(manifest);
		}
	}

	#endregion

	#region Baking the manifest

	[Test]
	public void TheBuilderCollectsNodesStoredInsideGraphs() {
		var graph = CreateAsset<ActionVerb>("Verb");
		var node = AddSubAsset<PrintTextNode>(graph, "node");

		var collected = AssetManifestBuilder.Collect(null).ToList();

		CollectionAssert.Contains(collected, graph);
		CollectionAssert.Contains(collected, node, "a suspended runner points at a node, so the bake has to reach sub-assets");
	}

	[Test]
	public void TheBuilderCollectsSubclassesDeclaredOutsideNarramancer() {
		// A game's own node types live in the game's assembly but are still Narramancer content, so the
		// scan matches on the base chain rather than on the declaring assembly alone.
		var graph = CreateAsset<ActionVerb>("Verb");
		var node = AddSubAsset<RecordingTestNode>(graph, "custom");
		Assume.That(typeof(RecordingTestNode).Assembly, Is.Not.EqualTo(typeof(NarramancerSingleton).Assembly),
			"precondition: the test node really is declared in another assembly");

		var collected = AssetManifestBuilder.Collect(null).ToList();

		CollectionAssert.Contains(collected, node);
	}

	[Test]
	public void AuthoredAdditionalAssetsAreIncluded() {
		// Nothing can discover a game's sprites by scanning for Narramancer types, so they are listed by hand.
		var texture = new Texture2D(2, 2);
		var manifest = ScriptableObject.CreateInstance<AssetManifest>();
		manifest.AdditionalAssets.Add(texture);

		try {
			CollectionAssert.Contains(AssetManifestBuilder.Collect(manifest).ToList(), texture);
		}
		finally {
			Object.DestroyImmediate(manifest);
			Object.DestroyImmediate(texture);
		}
	}

	[Test]
	public void BakingProducesAnEntryForEveryCollectedAsset() {
		var graph = CreateAsset<ActionVerb>("Verb");
		AddSubAsset<PrintTextNode>(graph, "node");
		var manifest = ScriptableObject.CreateInstance<AssetManifest>();

		try {
			AssetManifestBuilder.Populate(manifest);

			Assert.Greater(manifest.Count, 0);
			CollectionAssert.AllItemsAreUnique(manifest.Entries.Select(entry => entry.id).ToList(),
				"two assets sharing an id would silently resolve to each other");
			foreach (var entry in manifest.Entries) {
				Assert.IsNotNull(entry.asset, $"entry '{entry.id}' was baked with no asset behind it");
			}
		}
		finally {
			Object.DestroyImmediate(manifest);
		}
	}

	#endregion

	#region End to end

	/// <summary>
	/// The whole point, assembled: a story that points at real node assets is serialized and read back, and
	/// the references come out as the same assets. This is the scenario the Odin path gets wrong after a
	/// patch, and it is the test that has to keep passing once the swap lands.
	/// </summary>
	[Test]
	public void AStorySerializedThroughTheResolverKeepsItsAssetReferences() {
		var graph = CreateAsset<ActionVerb>("Verb");
		var waiter = AddSubAsset<SuspendingTestNode>(graph, "waiter");
		var after = AddSubAsset<RecordingTestNode>(graph, "after");
		var noun = CreateAsset<NounScriptableObject>("Wren");

		var story = new StoryInstance();
		story.CreateInstance(noun);
		var runner = new NodeRunner { name = "main" };
		runner.Start(waiter, after);
		runner.Update();
		story.NodeRunners["main"] = runner;

		var serializer = new NarramancerSerializer(new UnityReferenceResolver(null));
		var json = serializer.Serialize(story);
		var restored = serializer.Deserialize<StoryInstance>(json);

		StringAssert.DoesNotContain(waiter.GetInstanceID().ToString(), json, "the save must not carry a session-local id");
		Assert.AreSame(noun, restored.Instances[0].Noun);

		// The queue is protected, so the reference is observed by resuming into it: if the saved node
		// reference had resolved to anything other than this asset, its run count would stay at zero.
		restored.NodeRunners["main"].Resume();
		PumpUntilIdle(restored.NodeRunners["main"]);
		Assert.AreEqual(1, after.runCount, "the node the runner was suspended in front of must come back as the same asset");
	}

	/// <summary>
	/// The same story resolved the way a <b>build</b> does — through the baked manifest alone, with the asset
	/// database taken out of the picture. Nothing else covers the path that actually ships.
	/// </summary>
	[Test]
	public void TheBakedManifestAloneIsEnoughToResolveASave() {
		var graph = CreateAsset<ActionVerb>("Verb");
		var node = AddSubAsset<RecordingTestNode>(graph, "node");
		var manifest = ScriptableObject.CreateInstance<AssetManifest>();
		AssetManifestBuilder.Populate(manifest);

		try {
			var story = new StoryInstance();
			var runner = new NodeRunner { name = "main" };
			runner.Start(node);
			story.NodeRunners["main"] = runner;

			var writer = new NarramancerSerializer(new UnityReferenceResolver(manifest));
			var json = writer.Serialize(story);

			// ManifestOnlyResolver stands in for a player, where AssetDatabase does not exist.
			var player = new NarramancerSerializer(new ManifestOnlyResolver(manifest));
			var restored = player.Deserialize<StoryInstance>(json);
			PumpUntilIdle(restored.NodeRunners["main"]);

			Assert.AreEqual(1, node.runCount, "the manifest alone must be enough to find the node again");
		}
		finally {
			Object.DestroyImmediate(manifest);
		}
	}

	/// <summary>The resolver a build effectively has: the manifest and nothing else.</summary>
	private sealed class ManifestOnlyResolver : IReferenceResolver {

		private readonly AssetManifest manifest;

		public ManifestOnlyResolver(AssetManifest manifest) {
			this.manifest = manifest;
		}

		public bool TryGetId(Object asset, out string id) => manifest.TryGetId(asset, out id);

		public bool TryResolve(string id, System.Type type, out Object asset) => manifest.TryResolve(id, type, out asset);
	}

	#endregion
}
