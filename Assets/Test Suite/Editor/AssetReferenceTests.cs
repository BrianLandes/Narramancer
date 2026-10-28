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

	/// <summary>A Resources folder under the test directory, so the <c>res:</c> id form can be exercised.</summary>
	private string ResourceDirectory => $"{TestDirectory}/Resources";

	[SetUp]
	public void SetUp() {
		RecordingTestNode.Log.Clear();
		RuntimeAssetRegistry.Clear();
		if (!AssetDatabase.IsValidFolder(TestDirectory)) {
			AssetDatabase.CreateFolder("Assets", Path.GetFileName(TestDirectory));
		}
	}

	[TearDown]
	public void TearDown() {
		RuntimeAssetRegistry.Clear();
		if (AssetDatabase.IsValidFolder(TestDirectory)) {
			AssetDatabase.DeleteAsset(TestDirectory);
		}
	}

	/// <summary>Writes an asset inside a Resources folder, so it gets a <c>res:</c> id.</summary>
	private T CreateResourceAsset<T>(string name) where T : ScriptableObject {
		if (!AssetDatabase.IsValidFolder(ResourceDirectory)) {
			AssetDatabase.CreateFolder(TestDirectory, "Resources");
		}
		var asset = ScriptableObject.CreateInstance<T>();
		asset.name = name;
		AssetDatabase.CreateAsset(asset, $"{ResourceDirectory}/{name}.asset");
		return asset;
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

	#region Resources — the zero-setup path for a game's own assets

	/// <summary>
	/// An asset under a Resources folder is named by its path, not its GUID, because
	/// <c>Resources.Load</c> works in a build with no bake and no bookkeeping. That is the whole point: a
	/// game can drop a sprite in Resources, put it in a blackboard, and saving it works with no setup.
	/// </summary>
	[Test]
	public void AssetsUnderResourcesAreNamedByPath() {
		var noun = CreateResourceAsset<NounScriptableObject>("Wren");

		Assert.IsTrue(AssetIds.TryGetId(noun, out var id));

		Assert.IsTrue(AssetIds.IsResourceId(id), $"expected a resource id, got '{id}'");
		Assert.AreEqual($"{AssetIds.ResourcePrefix}Wren", id, "the path is relative to the Resources folder and drops the extension");
	}

	[Test]
	public void AResourceIdResolvesBackToTheSameAsset() {
		var noun = CreateResourceAsset<NounScriptableObject>("Wren");
		AssetIds.TryGetId(noun, out var id);

		Assert.IsTrue(AssetIds.TryResolve(id, out var resolved));

		Assert.AreSame(noun, resolved);
	}

	[Test]
	public void AResourceIdResolvesWithoutTheAssetDatabase() {
		// The reason this form exists: Resources.Load is available in a player, so no manifest entry and no
		// AssetDatabase are needed to find the asset again.
		var noun = CreateResourceAsset<NounScriptableObject>("Wren");
		AssetIds.TryGetId(noun, out var id);

		Assert.AreSame(noun, Resources.Load(id.Substring(AssetIds.ResourcePrefix.Length)));
	}

	[Test]
	public void AResourceIdForSomethingThatIsNoLongerThereDoesNotResolve() {
		Assert.IsFalse(AssetIds.TryResolve($"{AssetIds.ResourcePrefix}NoSuchAsset", out _));
	}

	[Test]
	public void AResourceIdIsNotMistakenForAGuidId() {
		Assert.IsFalse(AssetIds.TrySplit($"{AssetIds.ResourcePrefix}Some/Path", out _, out _),
			"both forms contain a colon, so the prefix has to be what tells them apart");
	}

	[Test]
	public void AssetsOutsideResourcesStillUseTheGuidForm() {
		var noun = CreateAsset<NounScriptableObject>("Wren");

		Assert.IsTrue(AssetIds.TryGetId(noun, out var id));

		Assert.IsFalse(AssetIds.IsResourceId(id));
		Assert.IsTrue(AssetIds.TrySplit(id, out _, out _));
	}

	[Test]
	public void TheBakeIncludesEverythingUnderResources() {
		// Free in build size, since Resources content ships wholesale anyway — and it is what gives a *build*
		// the reverse lookup it needs to mint a res: id when saving.
		var noun = CreateResourceAsset<NounScriptableObject>("Wren");
		var manifest = ScriptableObject.CreateInstance<AssetManifest>();

		try {
			AssetManifestBuilder.Populate(manifest);

			Assert.IsTrue(manifest.TryGetId(noun, out var id));
			Assert.IsTrue(AssetIds.IsResourceId(id));
		}
		finally {
			Object.DestroyImmediate(manifest);
		}
	}

	[Test]
	public void TheBakeIncludesWhatNarramancerContentReferences() {
		// The sample's equipment sprites: outside Resources, not a Narramancer type, but handed to an Image by a
		// verb. Missing from the manifest, a build could not name them, so their Images loaded as the scene had them.
		var texture = new Texture2D(2, 2) { name = "Portrait" };
		AssetDatabase.CreateAsset(texture, $"{TestDirectory}/Portrait.asset");
		var unreferenced = new Texture2D(2, 2) { name = "Unused" };
		AssetDatabase.CreateAsset(unreferenced, $"{TestDirectory}/Unused.asset");
		var graph = CreateAsset<ActionVerb>("Verb");
		AddSubAsset<UnityObjectNode>(graph, "portrait").SetObject(texture);
		AssetDatabase.SaveAssets();
		var manifest = ScriptableObject.CreateInstance<AssetManifest>();

		try {
			AssetManifestBuilder.Populate(manifest);

			Assert.IsTrue(manifest.TryGetId(texture, out _), "an asset a verb references must be nameable in a build");
			Assert.IsFalse(manifest.TryGetId(unreferenced, out _), "an asset nothing references stays out of the manifest");
		}
		finally {
			Object.DestroyImmediate(manifest);
		}
	}

	#endregion

	#region The runtime registry — the last resort

	/// <summary>
	/// For an object that reaches a blackboard from game code rather than from a node or a Resources folder.
	/// The author supplies the name, and it is stored in the save verbatim.
	/// </summary>
	[Test]
	public void RegisteredAssetsAreNamedByTheirKey() {
		var noun = CreateAsset<NounScriptableObject>("Wren");
		RuntimeAssetRegistry.Register("hero", noun);

		var resolver = new UnityReferenceResolver(null);

		Assert.IsTrue(resolver.TryGetId(noun, out var id));
		Assert.AreEqual($"{RuntimeAssetRegistry.Prefix}hero", id);
		Assert.IsTrue(resolver.TryResolve(id, typeof(NounScriptableObject), out var resolved));
		Assert.AreSame(noun, resolved);
	}

	[Test]
	public void TheRegistryBeatsTheAssetDatabase() {
		// An explicit registration is the author saying "this asset, under this name", which should win over
		// anything inferred — otherwise registering something would appear to do nothing.
		var noun = CreateAsset<NounScriptableObject>("Wren");
		RuntimeAssetRegistry.Register("hero", noun);

		Assert.IsTrue(new UnityReferenceResolver(null).TryGetId(noun, out var id));

		Assert.IsFalse(AssetIds.TrySplit(id, out _, out _), "the GUID form should not have been chosen");
	}

	[Test]
	public void ReRegisteringAKeyPointsItAtTheNewAsset() {
		// So a game can swap an asset out between sessions without invalidating saves that name it.
		var first = CreateAsset<NounScriptableObject>("First");
		var second = CreateAsset<NounScriptableObject>("Second");
		RuntimeAssetRegistry.Register("hero", first);
		RuntimeAssetRegistry.Register("hero", second);

		var resolver = new UnityReferenceResolver(null);

		Assert.IsTrue(resolver.TryResolve($"{RuntimeAssetRegistry.Prefix}hero", null, out var resolved));
		Assert.AreSame(second, resolved);
		Assert.IsFalse(RuntimeAssetRegistry.TryGetId(first, out _), "the displaced asset must not still claim the key");
	}

	[Test]
	public void AnUnregisteredKeyDoesNotResolve() {
		Assert.IsFalse(new UnityReferenceResolver(null).TryResolve($"{RuntimeAssetRegistry.Prefix}nobody", null, out _));
	}

	/// <summary>
	/// The case the registry exists for, end to end: an object with no asset identity at all — created at
	/// runtime — still survives a save, because the game named it.
	/// </summary>
	[Test]
	public void ARuntimeObjectSurvivesASaveOnceItIsRegistered() {
		var runtimeOnly = ScriptableObject.CreateInstance<NounScriptableObject>();
		runtimeOnly.name = "Conjured";
		try {
			Assume.That(AssetIds.TryGetId(runtimeOnly, out _), Is.False, "precondition: it has no asset identity");
			RuntimeAssetRegistry.Register("conjured", runtimeOnly);

			var story = new StoryInstance();
			story.Blackboard.Set("noun", runtimeOnly);

			var serializer = new NarramancerSerializer(new UnityReferenceResolver(null));
			var restored = serializer.Deserialize<StoryInstance>(serializer.Serialize(story));

			Assert.AreSame(runtimeOnly, restored.Blackboard.Get<NounScriptableObject>("noun"));
		}
		finally {
			Object.DestroyImmediate(runtimeOnly);
		}
	}

	#endregion

	#region Blackboard keys — the second place instanceIDs were hiding

	/// <summary>
	/// <c>Blackboard.UniqueKey</c> scopes a key to its owner, and used to do it with
	/// <c>owner.GetHashCode()</c> — which on a <c>UnityEngine.Object</c> <b>is the instanceID</b>. So every
	/// loop index, spawned object, timer and chosen element stored under one of these keys silently changed
	/// meaning when the build changed, and the state read back as a default. The same bug as the saved
	/// references, in a second place, found while sizing M2.
	/// </summary>
	[Test]
	public void ABlackboardKeyIsScopedByDurableIdNotInstanceId() {
		var graph = CreateAsset<ActionVerb>("Verb");
		var node = AddSubAsset<PrintTextNode>(graph, "node");
		Assume.That(node.GetHashCode(), Is.EqualTo(node.GetInstanceID()),
			"precondition: Unity's GetHashCode really is the instanceID — the whole reason for this fix");

		var key = Blackboard.UniqueKey(node, "index");

		AssetIds.TryGetId(node, out var durableId);
		StringAssert.Contains(durableId, key, "the key should carry the asset's durable id");
		StringAssert.DoesNotContain(node.GetInstanceID().ToString(), key,
			"a session-local instanceID must not scope anything that gets saved");
	}

	[Test]
	public void TwoNodesGetDifferentBlackboardKeys() {
		var graph = CreateAsset<ActionVerb>("Verb");
		var first = AddSubAsset<PrintTextNode>(graph, "first");
		var second = AddSubAsset<PrintTextNode>(graph, "second");

		Assert.AreNotEqual(Blackboard.UniqueKey(first, "index"), Blackboard.UniqueKey(second, "index"),
			"two nodes in one graph must not collide, or they would overwrite each other's private state");
	}

	[Test]
	public void TheSameNodeAlwaysGetsTheSameBlackboardKey() {
		var graph = CreateAsset<ActionVerb>("Verb");
		var node = AddSubAsset<PrintTextNode>(graph, "node");

		Assert.AreEqual(Blackboard.UniqueKey(node, "index"), Blackboard.UniqueKey(node, "index"));
	}

	/// <summary>
	/// State keyed to a node survives the save/load boundary — the thing that was actually broken. A key built
	/// from a durable id still matches after the graph has been through the serializer.
	/// </summary>
	[Test]
	public void StateKeyedToANodeSurvivesARoundTrip() {
		var graph = CreateAsset<ActionVerb>("Verb");
		var node = AddSubAsset<PrintTextNode>(graph, "node");
		var story = new StoryInstance();
		story.Blackboard.Set(Blackboard.UniqueKey(node, "index"), 7);

		var serializer = new NarramancerSerializer(new UnityReferenceResolver(null));
		var restored = serializer.Deserialize<StoryInstance>(serializer.Serialize(story));

		Assert.AreEqual(7, restored.Blackboard.Get<int>(Blackboard.UniqueKey(node, "index")),
			"a half-finished loop, a spawned object or a running timer all live under keys like this one");
	}

	/// <summary>
	/// An object with no durable id still works for the session, but says so — silently falling back to the
	/// broken key is how this went unnoticed in the first place.
	/// </summary>
	[Test]
	public void AnUnregisterableOwnerFallsBackButWarns() {
		var loose = ScriptableObject.CreateInstance<PrintTextNode>();
		loose.name = "Loose";
		try {
			UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Warning,
				new System.Text.RegularExpressions.Regex("Loose.*durable id"));

			Assert.IsNotEmpty(Blackboard.UniqueKey(loose, "index"));
		}
		finally {
			Object.DestroyImmediate(loose);
		}
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
