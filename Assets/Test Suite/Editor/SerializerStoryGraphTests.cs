using System;
using System.Collections.Generic;
using Narramancer;
using Narramancer.Serialization;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// The replacement serializer against the <b>real</b> story graph, rather than the hand-made types in
/// <see cref="SerializerTests"/>. This is the test net that decides whether the port is viable: the story
/// graph has polymorphism, cycles, dictionary subclasses, node assets and serialized lambda closures in
/// it, and no set of toy types exercises that combination convincingly.
/// <para>
/// Asset references go through a resolver that mints an id per object rather than reading the asset GUID.
/// That stands in for the real <c>UnityReferenceResolver</c> (docs/ODIN_REMOVAL_PLAN.md, phase 3): it uses
/// the same seam and proves the graph round-trips through it, while leaving where the id <em>comes from</em>
/// to that phase. Ids minted per session are exactly what makes the current Odin path unsafe, so this
/// stand-in must not outlive phase 3.
/// </para>
/// </summary>
public class SerializerStoryGraphTests {

	private readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();
	private MintingReferenceResolver references;
	private NarramancerSerializer serializer;

	[SetUp]
	public void SetUp() {
		RecordingTestNode.Log.Clear();
		references = new MintingReferenceResolver();
		serializer = new NarramancerSerializer(references);
	}

	[TearDown]
	public void TearDown() {
		foreach (var asset in created) {
			if (asset != null) {
				UnityEngine.Object.DestroyImmediate(asset);
			}
		}
		created.Clear();
	}

	/// <summary>Hands out an id for any Unity object the graph points at, and resolves it back again.</summary>
	private sealed class MintingReferenceResolver : IReferenceResolver {

		private readonly Dictionary<string, UnityEngine.Object> byId = new Dictionary<string, UnityEngine.Object>(StringComparer.Ordinal);
		private readonly Dictionary<UnityEngine.Object, string> idByAsset = new Dictionary<UnityEngine.Object, string>();

		public int Count => byId.Count;

		public bool TryGetId(UnityEngine.Object asset, out string id) {
			if (asset == null) {
				id = null;
				return false;
			}
			if (!idByAsset.TryGetValue(asset, out id)) {
				id = $"ref-{idByAsset.Count}";
				idByAsset[asset] = id;
				byId[id] = asset;
			}
			return true;
		}

		public bool TryResolve(string id, Type type, out UnityEngine.Object asset) {
			if (id != null && byId.TryGetValue(id, out asset) && (type == null || type.IsInstanceOfType(asset))) {
				return true;
			}
			asset = null;
			return false;
		}
	}

	private T Asset<T>(string name) where T : ScriptableObject {
		var asset = ScriptableObject.CreateInstance<T>();
		asset.name = name;
		created.Add(asset);
		return asset;
	}

	private T RoundTrip<T>(T value) {
		return serializer.Deserialize<T>(serializer.Serialize(value));
	}

	private static void PumpUntilIdle(NodeRunner runner, int maxSteps = 32) {
		var steps = 0;
		while (steps < maxSteps && runner.Update()) {
			steps++;
		}
		Assert.Less(steps, maxSteps, "runner did not settle — likely looping");
	}

	#region The graph survives

	[Test]
	public void AnEmptyStoryRoundTrips() {
		var restored = RoundTrip(new StoryInstance());

		Assert.IsNotNull(restored);
		Assert.IsNotNull(restored.Instances, "the instances list must not come back null");
		Assert.IsEmpty(restored.Instances);
	}

	[Test]
	public void BlackboardValuesSurvive() {
		var story = new StoryInstance();
		story.Blackboard.Set("chapter", 3);
		story.Blackboard.Set("hero", "Wren");
		story.Blackboard.Set("ratio", 0.5f);
		story.Blackboard.Set("flagged", true);

		var restored = RoundTrip(story);

		Assert.AreEqual(3, restored.Blackboard.Get<int>("chapter"));
		Assert.AreEqual("Wren", restored.Blackboard.Get<string>("hero"));
		Assert.AreEqual(0.5f, restored.Blackboard.Get<float>("ratio"));
		Assert.IsTrue(restored.Blackboard.Get<bool>("flagged"));
	}

	[Test]
	public void NounInstancesSurviveWithTheirIdentity() {
		var noun = Asset<NounScriptableObject>("Wren");
		var story = new StoryInstance();
		var instance = story.CreateInstance(noun);
		instance.DisplayName = "Wren the Wanderer";
		var originalUid = instance.UID.ToString();

		var restored = RoundTrip(story);

		Assert.AreEqual(1, restored.Instances.Count);
		Assert.AreEqual("Wren the Wanderer", restored.Instances[0].DisplayName);
		Assert.AreEqual(originalUid, restored.Instances[0].UID.ToString(), "the UID must survive — relationships key off it");
	}

	[Test]
	public void NounInstanceBlackboardsSurvive() {
		var noun = Asset<NounScriptableObject>("Wren");
		var story = new StoryInstance();
		story.CreateInstance(noun).Blackboard.Set("mood", "wary");

		var restored = RoundTrip(story);

		Assert.AreEqual("wary", restored.Instances[0].Blackboard.Get<string>("mood"));
	}

	[Test]
	public void TheNounTemplateComesBackAsTheSameAssetNotACopy() {
		// Inlining the template would bake a stale copy of authored content into the save, so a later edit
		// to the noun would never reach an existing save.
		var noun = Asset<NounScriptableObject>("Wren");
		var story = new StoryInstance();
		story.CreateInstance(noun);

		var restored = RoundTrip(story);

		Assert.AreSame(noun, restored.Instances[0].Noun);
	}

	#endregion

	#region Resume after load — the flagship

	/// <summary>
	/// The headline feature through the new serializer: a verb suspended part-way through survives the round
	/// trip and picks up exactly where it stopped. <see cref="NodeRunnerTests"/> asserts the same thing
	/// through the Odin path; both have to pass until the swap, and this one after it.
	/// </summary>
	[Test]
	public void ASuspendedRunnerResumesAfterARoundTrip() {
		var story = new StoryInstance();
		var runner = new NodeRunner { name = "main" };
		var waiter = Asset<SuspendingTestNode>("waiter");
		var after = Asset<RecordingTestNode>("after");

		runner.Start(waiter, after);
		runner.Update();
		story.NodeRunners["main"] = runner;

		Assert.AreEqual(1, waiter.runCount, "precondition: suspended after the first node");
		Assert.AreEqual(0, after.runCount, "precondition: the second node has not run");

		var restored = RoundTrip(story);

		Assert.IsTrue(restored.NodeRunners.ContainsKey("main"), "the runner must survive in the save");
		var restoredRunner = restored.NodeRunners["main"];
		Assert.AreNotSame(runner, restoredRunner, "the restored runner is a different object");

		restoredRunner.Resume();
		PumpUntilIdle(restoredRunner);

		Assert.AreEqual(1, after.runCount, "the node queued behind the suspend point must run after reload");
		CollectionAssert.AreEqual(new[] { "waiter", "after" }, RecordingTestNode.Log);
	}

	/// <summary>
	/// Negative control for the test above: a runner with nothing left must resume into nothing. Without it
	/// the resume test could pass because the queue was rebuilt from scratch rather than restored.
	/// </summary>
	[Test]
	public void ADrainedRunnerHasNothingToResumeIntoAfterARoundTrip() {
		var story = new StoryInstance();
		var runner = new NodeRunner { name = "main" };
		var only = Asset<RecordingTestNode>("only");

		runner.Start(only);
		PumpUntilIdle(runner);
		story.NodeRunners["main"] = runner;
		RecordingTestNode.Log.Clear();

		var restored = RoundTrip(story);
		restored.NodeRunners["main"].Resume();
		PumpUntilIdle(restored.NodeRunners["main"]);

		Assert.AreEqual(1, only.runCount, "the node must not run a second time");
		CollectionAssert.IsEmpty(RecordingTestNode.Log);
	}

	#endregion

	/// <summary>Holds a promise and the object its callback captured, so both cross the boundary together.</summary>
	private class PromiseHolder {
		public StringIntDictionary Box;
		public Promise Promise;
	}

	/// <summary>
	/// The continuation path, which is the most fragile thing in a save: <c>Promise.doneCallbacks</c> stores
	/// serialized lambda closures, and their captured state lives in compiler-generated fields. This asserts
	/// the whole chain — the closure survives, still resolves to a callable action, and the object it
	/// captured is the <em>same instance</em> the restored graph holds elsewhere rather than a second copy.
	/// <para>
	/// It passing does not make the closure model safe. The type name a closure is stored under is a compiler
	/// ordinal that moves when the node's source file is edited, which is Landmine B in
	/// docs/ODIN_REMOVAL_PLAN.md and the reason docs/CONTINUATION_DESIGN.md exists. This test pins the
	/// behaviour so the Continuation work has something to replace rather than guess at.
	/// </para>
	/// </summary>
	[Test]
	public void ASerializedCallbackSurvivesAndStillSeesWhatItCaptured() {
		var holder = new PromiseHolder { Box = new StringIntDictionary(), Promise = new Promise() };
		var captured = holder.Box;
		holder.Promise.WhenDone(() => captured["ran"] = 1);

		var restored = RoundTrip(holder);
		restored.Promise.Resolve();

		Assert.IsTrue(restored.Box.ContainsKey("ran"), "the restored callback did not run against the restored graph");
		Assert.AreEqual(1, restored.Box["ran"]);
		Assert.IsFalse(captured.ContainsKey("ran"), "the restored callback must not still be pointing at the original object");
	}

	#region Every reference is accounted for

	/// <summary>
	/// Nothing in the graph is silently lost. Since the swap, a Unity object the resolver cannot name is
	/// dropped with a logged error rather than throwing — the right call at save time, but it means a
	/// regression could quietly shrink a save instead of failing it. This is the test that would catch that.
	/// <para>
	/// The two tests that used to live here compared reference counts and payload size against the Odin path.
	/// Both became tautological when Odin stopped being the serializer, so they were replaced by this.
	/// </para>
	/// </summary>
	[Test]
	public void EveryAssetInTheGraphIsWrittenAsAHandleAndNoneAreDropped() {
		var story = StoryWithSomeOfEverything();

		var json = serializer.Serialize(story);

		Assert.AreEqual(3, references.Count, "the graph points at a noun and two nodes");
		var handles = json.Split(new[] { NarramancerSerializer.AssetKey }, StringSplitOptions.None).Length - 1;
		Assert.AreEqual(references.Count, handles,
			"every reference the resolver named should appear in the payload as a handle");
	}

	#endregion

	#region Fixtures

	private StoryInstance StoryWithSomeOfEverything() {
		var story = new StoryInstance();
		story.sceneIndex = 2;
		story.Blackboard.Set("chapter", 3);
		story.Blackboard.Set("hero", "Wren");
		story.AddTextLog("the forest thickens");

		var noun = Asset<NounScriptableObject>("Wren");
		story.CreateInstance(noun).Blackboard.Set("mood", "wary");

		var runner = new NodeRunner { name = "main" };
		runner.Start(Asset<SuspendingTestNode>("waiter"), Asset<RecordingTestNode>("after"));
		runner.Update();
		story.NodeRunners["main"] = runner;

		return story;
	}

	#endregion
}
