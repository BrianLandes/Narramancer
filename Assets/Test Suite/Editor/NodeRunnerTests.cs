using System.Collections.Generic;
using Narramancer;
using NUnit.Framework;
using UnityEngine;

/// <summary>A node that records that it ran. Enough to observe execution order and repetition.</summary>
public class RecordingTestNode : RunnableNode {
	/// <summary>Names of every node run since the last reset, in order, shared across nodes.</summary>
	public static readonly List<string> Log = new List<string>();

	public int runCount;

	public override void Run(NodeRunner runner) {
		runCount++;
		Log.Add(name);
	}
}

/// <summary>Records that it ran, then suspends the runner — the shape of every "wait for something" node.</summary>
public class SuspendingTestNode : RunnableNode {
	public int runCount;

	public override void Run(NodeRunner runner) {
		runCount++;
		RecordingTestNode.Log.Add(name);
		runner.Suspend();
	}
}

/// <summary>
/// Execution and, most importantly, <b>resume after a save/load boundary</b> — the behaviour the whole plugin
/// is sold on. The runner is driven by hand here: <c>Update()</c> is normally pumped by
/// <see cref="NarramancerSingleton"/>, but it is public and side-effect-free enough to step directly, which is
/// what makes this testable in EditMode at all.
/// </summary>
public class NodeRunnerTests {

	private readonly List<Object> created = new List<Object>();

	[SetUp]
	public void SetUp() {
		RecordingTestNode.Log.Clear();
	}

	[TearDown]
	public void TearDown() {
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
		return node;
	}

	/// <summary>Steps the runner until it stops making progress, with a guard against a runaway graph.</summary>
	private static int PumpUntilIdle(NodeRunner runner, int maxSteps = 32) {
		var steps = 0;
		while (steps < maxSteps && runner.Update()) {
			steps++;
		}
		Assert.Less(steps, maxSteps, "runner did not settle — likely looping");
		return steps;
	}

	#region Execution

	[Test]
	public void RunsQueuedNodesInOrder() {
		var runner = new NodeRunner();
		var first = Node<RecordingTestNode>("first");
		var second = Node<RecordingTestNode>("second");
		var third = Node<RecordingTestNode>("third");

		runner.Start(first, second, third);
		PumpUntilIdle(runner);

		CollectionAssert.AreEqual(new[] { "first", "second", "third" }, RecordingTestNode.Log);
		Assert.AreEqual(1, first.runCount);
		Assert.AreEqual(1, third.runCount);
	}

	[Test]
	public void RunnerGoesIdleOnceTheQueueDrains() {
		var runner = new NodeRunner();
		runner.Start(Node<RecordingTestNode>("only"));

		PumpUntilIdle(runner);

		Assert.IsFalse(runner.Update(), "a drained runner should report no further work");
		Assert.IsFalse(runner.IsRunning());
	}

	[Test]
	public void SuspendHaltsBeforeTheNextNode() {
		var runner = new NodeRunner();
		var waiter = Node<SuspendingTestNode>("waiter");
		var after = Node<RecordingTestNode>("after");

		runner.Start(waiter, after);
		runner.Update();

		Assert.AreEqual(1, waiter.runCount);
		Assert.AreEqual(0, after.runCount, "the node after a suspend must not run yet");
		Assert.IsFalse(runner.Update(), "a suspended runner reports no work until resumed");
		Assert.AreEqual(0, after.runCount);
	}

	[Test]
	public void ResumeContinuesFromSuspension() {
		var runner = new NodeRunner();
		var waiter = Node<SuspendingTestNode>("waiter");
		var after = Node<RecordingTestNode>("after");

		runner.Start(waiter, after);
		runner.Update();
		Assert.AreEqual(0, after.runCount);

		runner.Resume();
		PumpUntilIdle(runner);

		Assert.AreEqual(1, after.runCount);
		CollectionAssert.AreEqual(new[] { "waiter", "after" }, RecordingTestNode.Log);
	}

	[Test]
	public void ResumeWithANodeInsertsItFirst() {
		var runner = new NodeRunner();
		var waiter = Node<SuspendingTestNode>("waiter");
		var after = Node<RecordingTestNode>("after");
		var injected = Node<RecordingTestNode>("injected");

		runner.Start(waiter, after);
		runner.Update();

		runner.Resume(injected);
		PumpUntilIdle(runner);

		CollectionAssert.AreEqual(new[] { "waiter", "injected", "after" }, RecordingTestNode.Log);
	}

	[Test]
	public void StopAndResetAbandonsQueuedWork() {
		var runner = new NodeRunner();
		var waiter = Node<SuspendingTestNode>("waiter");
		var after = Node<RecordingTestNode>("after");

		runner.Start(waiter, after);
		runner.Update();
		runner.StopAndReset();

		runner.Resume();
		PumpUntilIdle(runner);

		Assert.AreEqual(0, after.runCount, "resuming a stopped runner must not run abandoned nodes");
	}

	#endregion

	#region Surviving a save

	private static StoryInstance RoundTrip(StoryInstance story) {
		var payload = SaveLoadUtilities.SerializePayload(story, out var objects);
		return SaveLoadUtilities.DeserializePayload<StoryInstance>(payload, objects);
	}

	/// <summary>
	/// The headline feature, end to end: a verb suspended part-way through survives serialization and picks up
	/// exactly where it stopped. If only one test in this repo is kept, keep this one.
	/// </summary>
	[Test]
	public void SuspendedRunnerResumesAfterARoundTrip() {
		var story = new StoryInstance();
		var runner = new NodeRunner { name = "main" };
		var waiter = Node<SuspendingTestNode>("waiter");
		var after = Node<RecordingTestNode>("after");

		runner.Start(waiter, after);
		runner.Update();
		story.NodeRunners["main"] = runner;

		Assert.AreEqual(1, waiter.runCount, "precondition: suspended after the first node");
		Assert.AreEqual(0, after.runCount, "precondition: the second node has not run");

		var restored = RoundTrip(story);

		Assert.IsTrue(restored.NodeRunners.ContainsKey("main"), "the runner should survive in the save");
		var restoredRunner = restored.NodeRunners["main"];
		Assert.AreNotSame(runner, restoredRunner, "the restored runner is a different object");

		restoredRunner.Resume();
		PumpUntilIdle(restoredRunner);

		Assert.AreEqual(1, after.runCount,
			"the node queued behind the suspend point must run after reload — this is resume-after-load");
		CollectionAssert.AreEqual(new[] { "waiter", "after" }, RecordingTestNode.Log);
	}

	/// <summary>
	/// Negative control for the test above. If <c>queuedNodes</c> did not really survive serialization, the
	/// resume test could still pass for the wrong reason — so prove the queue is what drives it by restoring a
	/// runner that legitimately has nothing left, and showing nothing runs.
	/// </summary>
	[Test]
	public void DrainedRunnerHasNothingToResumeIntoAfterARoundTrip() {
		var story = new StoryInstance();
		var runner = new NodeRunner { name = "main" };
		var only = Node<RecordingTestNode>("only");

		runner.Start(only);
		PumpUntilIdle(runner);
		story.NodeRunners["main"] = runner;

		// Ignore the pre-save run; we only care what happens after restoring.
		only.runCount = 0;
		RecordingTestNode.Log.Clear();

		var restored = RoundTrip(story).NodeRunners["main"];
		restored.Resume();
		PumpUntilIdle(restored);

		Assert.AreEqual(0, only.runCount, "a drained runner has nothing queued to resume into");
		CollectionAssert.IsEmpty(RecordingTestNode.Log);
	}

	[Test]
	public void RunnerBlackboardSurvivesARoundTrip() {
		var story = new StoryInstance();
		var runner = new NodeRunner { name = "main" };
		runner.Blackboard.Set("progress", 7);
		runner.Blackboard.Set("speaker", "Wren");
		story.NodeRunners["main"] = runner;

		var restored = RoundTrip(story).NodeRunners["main"];

		Assert.AreEqual(7, restored.Blackboard.Get<int>("progress"));
		Assert.AreEqual("Wren", restored.Blackboard.Get<string>("speaker"));
	}

	/// <summary>
	/// The run history drives the play-mode "which node is running" highlight, so it has to survive a reload
	/// too — otherwise the editor goes blank after loading a save.
	/// </summary>
	[Test]
	public void RunHistorySurvivesARoundTrip() {
		var story = new StoryInstance();
		var runner = new NodeRunner { name = "main" };
		var waiter = Node<SuspendingTestNode>("waiter");

		runner.Start(waiter);
		runner.Update();
		story.NodeRunners["main"] = runner;

		var restored = RoundTrip(story).NodeRunners["main"];

		Assert.IsTrue(restored.TryGetLastNodeEvent(waiter, out var nodeEvent),
			"the node's run event should survive, keyed to the same node asset");
		Assert.AreEqual("Ran", nodeEvent.name);
	}

	[Test]
	public void MultipleRunnersSurviveIndependently() {
		var story = new StoryInstance();
		var alpha = new NodeRunner { name = "alpha" };
		var beta = new NodeRunner { name = "beta" };
		alpha.Blackboard.Set("which", "alpha");
		beta.Blackboard.Set("which", "beta");
		story.NodeRunners["alpha"] = alpha;
		story.NodeRunners["beta"] = beta;

		var restored = RoundTrip(story);

		Assert.AreEqual(2, restored.NodeRunners.Count);
		Assert.AreEqual("alpha", restored.NodeRunners["alpha"].Blackboard.Get<string>("which"));
		Assert.AreEqual("beta", restored.NodeRunners["beta"].Blackboard.Get<string>("which"));
	}

	#endregion
}
