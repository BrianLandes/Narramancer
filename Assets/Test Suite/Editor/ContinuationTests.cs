using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Narramancer;
using Narramancer.Serialization;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Continuations: what a promise does when it resolves, stored as data instead of as a C# closure.
/// <para>
/// The bug being removed: a lambda's identity is compiler-generated, and the ordinal in
/// <c>&lt;&gt;c__DisplayClass22_0</c> moves when the source file is edited. <c>PrintTextNode</c> has been
/// saved under three different ordinals across real save files, which means editing a node could silently
/// invalidate saved games. Nothing in a <see cref="Continuation"/> is invented by the compiler.
/// </para>
/// </summary>
public class ContinuationTests {

	private readonly List<Object> created = new List<Object>();
	private StoryInstance previousStory;

	private static readonly FieldInfo StoryField =
		typeof(NarramancerSingleton).GetField("storyInstance", BindingFlags.Instance | BindingFlags.NonPublic);

	private static void SetStory(StoryInstance story) => StoryField.SetValue(NarramancerSingleton.Instance, story);
	private static StoryInstance GetStory() => (StoryInstance)StoryField.GetValue(NarramancerSingleton.Instance);

	[SetUp]
	public void SetUp() {
		previousStory = GetStory();
		SetStory(new StoryInstance());
		RuntimeAssetRegistry.Clear();
		RecordingTestNode.Log.Clear();
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

	private static StoryInstance RoundTrip(StoryInstance story) {
		var serializer = new NarramancerSerializer(new UnityReferenceResolver(null));
		return serializer.Deserialize<StoryInstance>(serializer.Serialize(story));
	}

	#region Each kind does what it says

	[Test]
	public void ResumeResumesTheNamedRunner() {
		var runner = Runner("main");
		var waiter = Node<SuspendingTestNode>("waiter");
		var after = Node<RecordingTestNode>("after");
		runner.Start(waiter, after);
		runner.Update();

		Continuation.Resume(runner).Invoke();
		runner.Update();

		Assert.AreEqual(1, after.runCount);
	}

	[Test]
	public void SetBoolWritesToTheRunnersBlackboard() {
		var runner = Runner("main");

		Continuation.SetBool(runner, "flag", true).Invoke();

		Assert.IsTrue(runner.Blackboard.GetBool("flag"));
	}

	[Test]
	public void ReleaseRemovesTheRunnerFromTheStory() {
		var parent = Runner("parent");
		var child = NarramancerSingleton.Instance.CreateSubRunner("child", parent);

		Continuation.Release(child).Invoke();

		Assert.IsNull(NarramancerSingleton.Instance.GetNodeRunner(child.name));
	}

	[Test]
	public void ANodeCallbackReachesTheNodeWithTheAuthorsId() {
		var runner = Runner("main");
		var node = Node<CallbackRecordingNode>("node");

		Continuation.NodeCallbackTo(node, "poll", runner).Invoke();

		Assert.AreEqual("poll", node.lastCallbackId, "the id is chosen by the node's author, not the compiler");
		Assert.AreSame(runner, node.lastRunner);
	}

	/// <summary>A node that records what it was called back with.</summary>
	private class CallbackRecordingNode : RunnableNode {
		public string lastCallbackId;
		public NodeRunner lastRunner;

		public override void Run(NodeRunner runner) {
		}

		public override void OnContinuation(string callbackId, NodeRunner runner) {
			lastCallbackId = callbackId;
			lastRunner = runner;
		}
	}

	#endregion

	#region Missing things are not errors

	/// <summary>
	/// A continuation names its runner by key, and by the time it fires that runner may legitimately be gone —
	/// released, or the story loaded past the point it mattered. That is the same situation the old closure
	/// was in when it held a reference nobody else pointed at, and it must stay quiet rather than throw.
	/// </summary>
	[Test]
	public void AContinuationForARunnerThatIsGoneDoesNothing() {
		var runner = Runner("main");
		var continuation = Continuation.Resume(runner);
		NarramancerSingleton.Instance.ReleaseNodeRunner(runner);

		Assert.DoesNotThrow(() => continuation.Invoke());
	}

	[Test]
	public void EveryKindToleratesAMissingRunner() {
		var runner = Runner("main");
		var node = Node<CallbackRecordingNode>("node");
		var continuations = new[] {
			Continuation.Resume(runner),
			Continuation.ResumeWith(runner, Node<RecordingTestNode>("next")),
			Continuation.Release(runner),
			Continuation.SetBool(runner, "flag", true),
			Continuation.NodeCallbackTo(node, "poll", runner),
		};
		NarramancerSingleton.Instance.ReleaseNodeRunner(runner);

		foreach (var continuation in continuations) {
			Assert.DoesNotThrow(() => continuation.Invoke(), $"{continuation.ContinuationKind} threw");
		}
	}

	#endregion

	#region Across the save boundary — the point of the exercise

	/// <summary>
	/// The flagship. A continuation survives the round trip and acts on the <b>restored</b> story, not on the
	/// objects that existed when it was created. That is what a closure could not do reliably: it captured
	/// object references, and its own type name moved whenever the source file was edited.
	/// </summary>
	[Test]
	public void AContinuationSurvivesASaveAndActsOnTheRestoredStory() {
		var story = GetStory();
		var runner = Runner("main");
		var promise = new Promise();
		promise.WhenDone(Continuation.SetBool(runner, "resumed", true));
		story.Promises["pending"] = promise;

		var restored = RoundTrip(story);
		SetStory(restored);
		restored.Promises["pending"].Resolve();

		Assert.IsTrue(restored.NodeRunners["main"].Blackboard.GetBool("resumed"),
			"the restored promise did not act on the restored runner");
		Assert.IsFalse(runner.Blackboard.GetBool("resumed"),
			"and it must not still be pointing at the runner from before the load");
	}

	/// <summary>
	/// The bug, asserted directly: no compiler-generated name reaches a save file any more. This is what
	/// changed between builds and silently broke saved games.
	/// </summary>
	[Test]
	public void NoCompilerGeneratedNameReachesTheSave() {
		var story = GetStory();
		var runner = Runner("main");
		var promise = new Promise();
		promise.WhenDone(Continuation.Resume(runner));
		story.Promises["pending"] = promise;

		var json = new NarramancerSerializer(new UnityReferenceResolver(null)).Serialize(story);

		StringAssert.DoesNotContain("DisplayClass", json);
		StringAssert.DoesNotContain("<>c", json);
		StringAssert.DoesNotContain("b__", json);
	}

	/// <summary>
	/// The deliberate asymmetry. A transient callback is a plain delegate and is <b>not</b> saved — which is
	/// why it has a different name rather than being an overload: a lambda binds to an <c>Action</c> parameter
	/// silently, so an overload would have let every existing call site keep compiling while quietly losing
	/// the ability to survive a save.
	/// </summary>
	[Test]
	public void ATransientCallbackDoesNotSurviveASave() {
		var story = GetStory();
		var ran = false;
		var promise = new Promise();
		promise.WhenDoneThisSession(() => ran = true);
		story.Promises["pending"] = promise;

		var restored = RoundTrip(story);
		restored.Promises["pending"].Resolve();

		Assert.IsFalse(ran, "a transient callback must not be carried across a save");
	}

	[Test]
	public void ATransientCallbackStillRunsInTheSameSession() {
		var ran = false;
		var promise = new Promise();
		promise.WhenDoneThisSession(() => ran = true);

		promise.Resolve();

		Assert.IsTrue(ran);
	}

	#endregion

	#region Promise mechanics

	[Test]
	public void ResolvingAnAlreadyResolvedPromiseRunsACallbackImmediately() {
		var runner = Runner("main");
		var promise = new Promise();
		promise.Resolve();

		promise.WhenDone(Continuation.SetBool(runner, "flag", true));

		Assert.IsTrue(runner.Blackboard.GetBool("flag"), "a callback added after resolution should run at once");
	}

	[Test]
	public void EachCallbackRunsOnlyOncePerResolve() {
		var runner = Runner("main");
		var counter = Node<CallbackRecordingNode>("counter");
		var promise = new Promise();
		promise.WhenDone(Continuation.NodeCallbackTo(counter, "once", runner));

		promise.Resolve();
		counter.lastCallbackId = null;
		promise.Resolve();

		Assert.IsNull(counter.lastCallbackId, "the callback list is cleared as it is invoked");
	}

	[Test]
	public void AllCallbacksRunInOrder() {
		var runner = Runner("main");
		var promise = new Promise();
		promise.WhenDone(Continuation.SetBool(runner, "first", true));
		promise.WhenDone(Continuation.SetBool(runner, "second", true));

		promise.Resolve();

		Assert.IsTrue(runner.Blackboard.GetBool("first"));
		Assert.IsTrue(runner.Blackboard.GetBool("second"));
	}

	#endregion
}
