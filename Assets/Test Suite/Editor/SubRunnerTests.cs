using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Narramancer;
using Narramancer.Serialization;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Sub-runner ownership: a runner that starts other runners is responsible for stopping them.
/// <para>
/// Before this, a node that spawned a sub-runner found it again by <b>rebuilding its name out of hash
/// codes</b>. That works exactly as long as nothing is saved. After a reload the name no longer matched, so
/// <c>ChoosePrioritizedBranchNode.Cancel</c> silently failed to stop its sub-runner (a leak — still ticking
/// against a blackboard nobody owns) and <c>ParallelNode.Cancel</c> called <c>StopAndReset()</c> on the null
/// it got back. Ownership is now recorded when the sub-runner is created, so nothing has to be reconstructed.
/// </para>
/// </summary>
public class SubRunnerTests {

	private readonly List<Object> created = new List<Object>();
	private StoryInstance previousStory;

	// storyInstance is a [SerializeField] with no public setter, and these tests need a live story for
	// CreateNodeRunner/GetNodeRunner. Same approach as PollingNodeCharacterizationTests.
	private static readonly FieldInfo StoryField =
		typeof(NarramancerSingleton).GetField("storyInstance", BindingFlags.Instance | BindingFlags.NonPublic);

	private static void SetStory(StoryInstance story) => StoryField.SetValue(NarramancerSingleton.Instance, story);
	private static StoryInstance GetStory() => (StoryInstance)StoryField.GetValue(NarramancerSingleton.Instance);

	[SetUp]
	public void SetUp() {
		previousStory = GetStory();
		SetStory(new StoryInstance());
		RecordingTestNode.Log.Clear();
		RuntimeAssetRegistry.Clear();
	}

	[TearDown]
	public void TearDown() {
		SetStory(previousStory);
		RuntimeAssetRegistry.Clear();
		foreach (var obj in created) {
			if (obj != null) {
				Object.DestroyImmediate(obj);
			}
		}
		created.Clear();
		RecordingTestNode.Log.Clear();
	}

	private T Node<T>(string name) where T : RunnableNode {
		var node = ScriptableObject.CreateInstance<T>();
		node.name = name;
		created.Add(node);
		RuntimeAssetRegistry.Register($"test-{created.Count}-{name}", node);
		return node;
	}

	private static NodeRunner Runner(string name) => NarramancerSingleton.Instance.CreateNodeRunner(name);

	private static NodeRunner SubRunner(string name, NodeRunner parent) =>
		NarramancerSingleton.Instance.CreateSubRunner(name, parent);

	#region Ownership

	[Test]
	public void ASubRunnerIsRecordedAgainstItsParent() {
		var parent = Runner("parent");

		var child = SubRunner("child", parent);

		CollectionAssert.Contains(parent.SubRunnerNames, child.name);
		CollectionAssert.AreEquivalent(new[] { child }, parent.GetSubRunners().ToList());
	}

	[Test]
	public void ATopLevelRunnerOwnsNothing() {
		Assert.IsEmpty(Runner("alone").SubRunnerNames);
	}

	[Test]
	public void ReleasingASubRunnerDetachesItFromItsOwner() {
		// Otherwise the owner keeps a name the story no longer has, which then gets saved and looked up on
		// the next cancel.
		var parent = Runner("parent");
		var child = SubRunner("child", parent);

		NarramancerSingleton.Instance.ReleaseNodeRunner(child);

		CollectionAssert.DoesNotContain(parent.SubRunnerNames, child.name);
		Assert.IsEmpty(parent.GetSubRunners().ToList());
	}

	/// <summary>
	/// The runner table is a dictionary and <c>Add</c> throws on a duplicate key.
	/// <c>RunActionVerbForDurationNode</c> names its sub-runner <c>"{node} - {graph}"</c> — the same string
	/// every time that node runs — so two concurrent instances of it used to be a crash.
	/// </summary>
	[Test]
	public void TwoRunnersAskingForTheSameNameBothGetOne() {
		var first = Runner("Run Verb - Chapter One");
		var second = Runner("Run Verb - Chapter One");

		Assert.AreNotEqual(first.name, second.name);
		Assert.AreSame(first, NarramancerSingleton.Instance.GetNodeRunner(first.name));
		Assert.AreSame(second, NarramancerSingleton.Instance.GetNodeRunner(second.name));
	}

	#endregion

	#region Stopping

	/// <summary>
	/// The leak this work exists to fix: stopping a runner mid-flight used to abandon everything it had
	/// started, each sub-runner still being ticked by <c>UpdateNodeRunners</c> against a blackboard nobody
	/// owned.
	/// </summary>
	[Test]
	public void StoppingARunnerStopsAndReleasesWhatItStarted() {
		var parent = Runner("parent");
		var child = SubRunner("child", parent);
		var waiter = Node<SuspendingTestNode>("waiter");
		var after = Node<RecordingTestNode>("after");
		child.Start(waiter, after);
		child.Update();
		Assume.That(waiter.runCount, Is.EqualTo(1), "precondition: the sub-runner suspended part-way through");
		Assume.That(after.runCount, Is.EqualTo(0), "precondition: it has work left to do");

		parent.StopAndReset();

		// Observed by behaviour, not by IsRunning(): that reports "actively running", so it is already false
		// for a *suspended* runner and would pass whether or not the stop did anything. A stopped runner has
		// had its queue cleared, so resuming it runs nothing.
		child.Resume();
		child.Update();
		Assert.AreEqual(0, after.runCount, "the sub-runner was left with queued work, so it was not stopped");

		Assert.IsNull(NarramancerSingleton.Instance.GetNodeRunner(child.name),
			"and it should be released from the story, or it is still ticked and still saved");
		Assert.IsEmpty(parent.SubRunnerNames);
	}

	[Test]
	public void StoppingARunnerStopsTheWholeTreeBeneathIt() {
		var parent = Runner("parent");
		var child = SubRunner("child", parent);
		var grandchild = SubRunner("grandchild", child);

		parent.StopAndReset();

		Assert.IsNull(NarramancerSingleton.Instance.GetNodeRunner(child.name));
		Assert.IsNull(NarramancerSingleton.Instance.GetNodeRunner(grandchild.name),
			"recursion has to reach the bottom, or a nested ParallelNode still leaks");
	}

	[Test]
	public void StoppingARunnerLeavesItsSiblingsAlone() {
		var parent = Runner("parent");
		var child = SubRunner("child", parent);
		var unrelated = Runner("unrelated");

		parent.StopAndReset();

		Assert.IsNull(NarramancerSingleton.Instance.GetNodeRunner(child.name));
		Assert.AreSame(unrelated, NarramancerSingleton.Instance.GetNodeRunner(unrelated.name),
			"only what the runner owns should be stopped");
	}

	[Test]
	public void StoppingARunnerTwiceIsHarmless() {
		var parent = Runner("parent");
		SubRunner("child", parent);

		parent.StopAndReset();

		Assert.DoesNotThrow(() => parent.StopAndReset());
	}

	#endregion

	#region Across the save boundary — the regression

	/// <summary>
	/// The flagship. Ownership is recorded, not recomputed, so cancelling after a reload works. This is
	/// exactly the case that used to throw: the old code rebuilt the sub-runner's name from
	/// <c>runner.GetHashCode()</c>, and a restored runner is a different object with a different hash, so the
	/// lookup returned null and <c>StopAndReset()</c> was called on it.
	/// </summary>
	[Test]
	public void CancellingAfterASaveAndLoadStillStopsTheSubRunner() {
		var story = GetStory();
		var parent = Runner("parent");
		var child = SubRunner("child", parent);
		child.Start(Node<SuspendingTestNode>("waiter"));
		child.Update();

		var serializer = new NarramancerSerializer(new UnityReferenceResolver(null));
		var restored = serializer.Deserialize<StoryInstance>(serializer.Serialize(story));
		SetStory(restored);

		var restoredParent = restored.NodeRunners[parent.name];
		var restoredChild = restored.NodeRunners[child.name];
		Assume.That(restoredParent, Is.Not.SameAs(parent), "precondition: these really are different objects");

		Assert.DoesNotThrow(() => restoredParent.StopAndReset(),
			"the old name-rebuilding lookup threw a NullReferenceException here");

		Assert.IsNull(NarramancerSingleton.Instance.GetNodeRunner(child.name),
			"the restored sub-runner should have been stopped and released, not merely found");
		Assert.IsEmpty(restoredParent.SubRunnerNames);
		Assert.IsNotNull(restoredChild, "sanity: the restored child existed to begin with");
	}

	[Test]
	public void OwnershipSurvivesASaveAndLoad() {
		var story = GetStory();
		var parent = Runner("parent");
		var child = SubRunner("child", parent);

		var serializer = new NarramancerSerializer(new UnityReferenceResolver(null));
		var restored = serializer.Deserialize<StoryInstance>(serializer.Serialize(story));
		SetStory(restored);

		CollectionAssert.Contains(restored.NodeRunners[parent.name].SubRunnerNames, child.name);
	}

	#endregion
}
