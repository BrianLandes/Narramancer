using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Narramancer;
using NUnit.Framework;
using UnityEngine;
using XNode;

/// <summary>
/// <b>Characterization</b> tests for the timer-driven polling nodes — they pin what these nodes do <i>today</i>
/// so the `Continuation` refactor (docs/CONTINUATION_DESIGN.md) can be verified rather than hoped at. These
/// nodes are the Tier 2 work: they carry a self-rescheduling local function and shared mutable state, which is
/// exactly what a data record cannot express, so they are the ones most likely to change behaviour under the
/// refactor.
/// <para>
/// Driving them works because <see cref="NarramancerSingleton.OnUpdate"/> is public and pumps timers then
/// runners, and a <c>MakeTimer(0)</c> fires on the very next pump — <c>Time.time</c> is frozen within an
/// EditMode frame, so a zero delay makes each pump exactly one poll tick.
/// </para>
/// </summary>
public class PollingNodeCharacterizationTests {

	private readonly List<Object> created = new List<Object>();
	private StoryInstance previousStory;
	private ActionVerb graph;

	// storyInstance is a [SerializeField] on the singleton asset, and there is no public setter. Tests need a
	// live story for MakeTimer/CreateNodeRunner, so set it directly and put the original back afterwards —
	// otherwise the Resources asset is left holding test state.
	private static readonly FieldInfo StoryField =
		typeof(NarramancerSingleton).GetField("storyInstance", BindingFlags.Instance | BindingFlags.NonPublic);

	private static void SetStory(StoryInstance story) => StoryField.SetValue(NarramancerSingleton.Instance, story);
	private static StoryInstance GetStory() => (StoryInstance)StoryField.GetValue(NarramancerSingleton.Instance);

	/// <summary>Sets a private [SerializeField] on a node — the inputs these nodes expose are not public.</summary>
	private static void SetField(object target, string field, object value) {
		var info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
		Assert.IsNotNull(info, $"field '{field}' not found on {target.GetType().Name} — the node's shape changed");
		info.SetValue(target, value);
	}

	[SetUp]
	public void SetUp() {
		previousStory = GetStory();
		SetStory(new StoryInstance());
		RecordingTestNode.Log.Clear();
		graph = ScriptableObject.CreateInstance<ActionVerb>();
		graph.name = "Test Verb";
		created.Add(graph);
	}

	[TearDown]
	public void TearDown() {
		SetStory(previousStory);
		foreach (var g in created.OfType<NodeGraph>()) {
			foreach (var node in g.nodes.ToList()) {
				if (node != null) {
					Object.DestroyImmediate(node);
				}
			}
		}
		foreach (var obj in created) {
			if (obj != null) {
				Object.DestroyImmediate(obj);
			}
		}
		created.Clear();
		RecordingTestNode.Log.Clear();
	}

	private T AddNode<T>(string name) where T : Node {
		var node = graph.AddNode(typeof(T)) as T;
		node.name = name;
		return node;
	}

	/// <summary>One pump of the whole system: timers resolve, then runners step.</summary>
	private static void Pump(int times = 1) {
		for (var i = 0; i < times; i++) {
			NarramancerSingleton.Instance.OnUpdate();
		}
	}

	#region WaitUntilNode

	private WaitUntilNode NewWaitUntil(bool condition, out RecordingTestNode after) {
		var node = AddNode<WaitUntilNode>("waitUntil");
		after = AddNode<RecordingTestNode>("after");
		SetField(node, "condition", condition);
		SetField(node, "refreshDelay", 0f);   // fire on the next pump
		node.ConnectDownstream(after);
		return node;
	}

	[Test]
	public void WaitUntil_WhenConditionAlreadyTrue_DoesNotSuspend() {
		var node = NewWaitUntil(true, out var after);
		var runner = NarramancerSingleton.Instance.CreateNodeRunner("main");

		runner.Start(node);
		Pump(4);

		Assert.AreEqual(1, after.runCount, "a satisfied condition should fall straight through to the next node");
	}

	[Test]
	public void WaitUntil_WhenConditionFalse_SuspendsAndKeepsPolling() {
		var node = NewWaitUntil(false, out var after);
		var runner = NarramancerSingleton.Instance.CreateNodeRunner("main");

		runner.Start(node);
		Pump(10);

		Assert.AreEqual(0, after.runCount, "must not advance while the condition is false");
		Assert.IsTrue(runner.Blackboard.GetBool(Blackboard.UniqueKey(node, "Running")),
			"the polling loop should still consider itself running");
	}

	[Test]
	public void WaitUntil_ResumesOnceTheConditionBecomesTrue() {
		var node = NewWaitUntil(false, out var after);
		var runner = NarramancerSingleton.Instance.CreateNodeRunner("main");

		runner.Start(node);
		Pump(3);
		Assert.AreEqual(0, after.runCount, "precondition: still waiting");

		SetField(node, "condition", true);
		Pump(3);

		Assert.AreEqual(1, after.runCount, "the next node should run exactly once after the condition flips");
	}

	[Test]
	public void WaitUntil_CancelStopsThePollingLoop() {
		var node = NewWaitUntil(false, out var after);
		var runner = NarramancerSingleton.Instance.CreateNodeRunner("main");

		runner.Start(node);
		Pump(2);

		node.Cancel(runner);
		Assert.IsFalse(runner.Blackboard.GetBool(Blackboard.UniqueKey(node, "Running")),
			"Cancel clears the running flag the poll loop checks");

		// Even if the condition becomes true afterwards, a cancelled loop must not resume the runner.
		SetField(node, "condition", true);
		Pump(5);
		Assert.AreEqual(0, after.runCount, "a cancelled wait must not resume");
	}

	/// <summary>
	/// The running flag is keyed with <see cref="Blackboard.UniqueKey"/>, which is built from
	/// <c>owner.GetHashCode()</c> — <b>not stable across sessions</b>. Pinned here because the refactor moves
	/// more state into this same mechanism, so the instability has to be fixed rather than inherited.
	/// </summary>
	[Test]
	public void WaitUntil_RunningFlagKeyIsBuiltFromAnUnstableHashCode() {
		var node = AddNode<WaitUntilNode>("waitUntil");
		var key = Blackboard.UniqueKey(node, "Running");

		StringAssert.Contains(node.GetHashCode().ToString(), key,
			"UniqueKey embeds the runtime hash code, so this key cannot survive a save/load");
	}

	#endregion

	#region RunActionVerbForDurationNode

	/// <summary>
	/// The duration path: with a zero duration the timer resolves before the sub-runner ever steps (timers are
	/// pumped ahead of runners), so the timeout branch wins. What matters for the refactor is that the
	/// follow-on node is prepended and resumed <b>exactly once</b> — that is the behaviour the
	/// <c>subRunnerStillRunning</c> guard exists to protect.
	/// </summary>
	[Test]
	public void RunForDuration_TimeoutPrependsTheNextNodeExactlyOnce() {
		var subVerb = ScriptableObject.CreateInstance<ActionVerb>();
		subVerb.name = "Sub Verb";
		created.Add(subVerb);
		var subRoot = subVerb.AddNode(typeof(RootNode)) as RootNode;
		subRoot.name = "root";
		var subBody = subVerb.AddNode(typeof(RecordingTestNode)) as RecordingTestNode;
		subBody.name = "subBody";
		subRoot.GetOutputPort(nameof(RootNode.runNode)).Connect(subBody.GetInputPort(RunnableNode.ThisNodeField));

		var node = AddNode<RunActionVerbForDurationNode>("forDuration");
		node.actionVerb = subVerb;
		SetField(node, "duration", 0f);
		var after = AddNode<RecordingTestNode>("after");
		node.GetOutputPort(nameof(RunActionVerbForDurationNode.thenRunNode))
			.Connect(after.GetInputPort(RunnableNode.ThisNodeField));

		var runner = NarramancerSingleton.Instance.CreateNodeRunner("main");
		runner.Start(node);
		Pump(8);

		Assert.AreEqual(1, after.runCount,
			"the follow-on node must run exactly once — twice would mean the double-fire guard regressed");
		Assert.AreEqual(1, RecordingTestNode.Log.Count(n => n == "after"));
	}

	/// <summary>
	/// Pins that the sub-runner is cleaned up rather than leaked. `ReleaseNodeRunner` removes it from the story,
	/// so a leak here would show up as a growing runner table across a session — and as junk in every save.
	/// </summary>
	[Test]
	public void RunForDuration_ReleasesItsSubRunner() {
		var subVerb = ScriptableObject.CreateInstance<ActionVerb>();
		subVerb.name = "Sub Verb";
		created.Add(subVerb);
		var subRoot = subVerb.AddNode(typeof(RootNode)) as RootNode;
		var subBody = subVerb.AddNode(typeof(RecordingTestNode)) as RecordingTestNode;
		subRoot.GetOutputPort(nameof(RootNode.runNode)).Connect(subBody.GetInputPort(RunnableNode.ThisNodeField));

		var node = AddNode<RunActionVerbForDurationNode>("forDuration");
		node.actionVerb = subVerb;
		SetField(node, "duration", 0f);

		var runner = NarramancerSingleton.Instance.CreateNodeRunner("main");
		runner.Start(node);
		Pump(8);

		var runners = NarramancerSingleton.Instance.StoryInstance.NodeRunners;
		Assert.AreEqual(1, runners.Count,
			$"only the main runner should remain, found: {string.Join(", ", runners.Keys)}");
	}

	#endregion
}
