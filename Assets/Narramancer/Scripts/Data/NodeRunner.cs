
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Narramancer {
	[Serializable]
	public class NodeRunner {

		#region Private Members

		enum RunStatus {
			Stopped,
			Running,
			Suspended,
		}

		[SerializeField]
		private RunStatus runStatus = RunStatus.Stopped;

		/// <summary> The current node be run. </summary>
		[SerializeField]
		protected RunnableNode runningNode;

		[NonSerialized]
		protected RunnableNode lastRunningNode;

		/// <summary> Nodes that are queued up to running next. </summary>
		[SerializeField]
		protected List<RunnableNode> queuedNodes = new List<RunnableNode>();


		[SerializeField]
		protected Promise promise = new Promise();

		[NonSerialized]
		private bool doPostRunLogic = false;

		enum PostNodeBehavior {
			Suspend,
			Continue,
		}

		[NonSerialized]
		private PostNodeBehavior postCurrentNodeBehavior;

		[SerializeField]
		protected Blackboard blackboard = new Blackboard();
		public Blackboard Blackboard { get => blackboard; set => blackboard = value; }

		[SerializeField]
		List<NodeRunnerEvent> recentlyRunNodes = new List<NodeRunnerEvent>();

		[SerializeField]
		public string name = "Node Runner";

		/// <summary>
		/// Runners this one started and is responsible for — a <see cref="ParallelNode"/>'s branches, or the
		/// verb a "run for a duration" node is driving. Held as <b>names</b>, which are the keys of the story's
		/// runner table.
		/// <para>
		/// Names rather than references, deliberately, and not a retreat to the old scheme. What was broken
		/// before was that nodes <em>reconstructed</em> a sub-runner's name from hash codes at cancel time, so
		/// the name stopped matching the moment the build or the session changed. A name that is <em>stored</em>
		/// travels with the save and is never recomputed — the same way the runner table itself has always been
		/// keyed.
		/// </para>
		/// <para>
		/// Holding <c>NodeRunner</c> references instead does not work: this class is also serialized by Unity
		/// (<c>NarramancerSingleton.storyInstance</c> is a <c>[SerializeField]</c>), and Unity serializes nested
		/// plain classes <em>by value</em> with a depth limit of 10. A list of runners inside a runner both
		/// blows that limit and, worse, would come back from a domain reload as copies rather than the same
		/// objects the runner table holds.
		/// </para>
		/// </summary>
		[SerializeField]
		private List<string> subRunnerNames = new List<string>();

		#endregion

		#region Sub-runners

		/// <summary>The names of the runners this one started and will stop when it stops.</summary>
		public IReadOnlyList<string> SubRunnerNames => subRunnerNames;

		/// <summary>Takes ownership of a runner, so stopping this one stops that one too.</summary>
		public void AddSubRunner(NodeRunner subRunner) {
			if (subRunner == null || subRunner == this || subRunner.name.IsNullOrEmpty() || subRunnerNames.Contains(subRunner.name)) {
				return;
			}
			subRunnerNames.Add(subRunner.name);
		}

		/// <summary>Gives up ownership of a runner without stopping it.</summary>
		public void RemoveSubRunner(NodeRunner subRunner) {
			if (subRunner != null) {
				subRunnerNames.Remove(subRunner.name);
			}
		}

		/// <summary>The runners this one owns that the story still has. Skips any already released.</summary>
		public IEnumerable<NodeRunner> GetSubRunners() {
			var singleton = NarramancerSingleton.Instance;
			if (singleton == null) {
				yield break;
			}
			foreach (var subRunnerName in subRunnerNames.ToArray()) {
				var subRunner = singleton.GetNodeRunner(subRunnerName);
				if (subRunner != null) {
					yield return subRunner;
				}
			}
		}

		/// <summary>
		/// Stops and releases every runner below this one, depth first. Called from
		/// <see cref="StopAndReset"/>: without it, stopping a runner mid-flight abandons everything it
		/// started, each still ticking against a blackboard nobody owns.
		/// </summary>
		private void StopSubRunners() {
			var singleton = NarramancerSingleton.Instance;
			foreach (var subRunner in GetSubRunners().ToArray()) {
				subRunner.StopAndReset();
				singleton.ReleaseNodeRunner(subRunner);
			}
			subRunnerNames.Clear();
		}

		#endregion

		#region Public Properties

		/// <summary> The current node being run. </summary>
		public RunnableNode CurrentNode => runningNode;

		public bool IsRunning() {
			return runStatus == RunStatus.Running && runningNode != null;
		}

		public bool verbose = false;

		#endregion

		#region Constructors

		public NodeRunner() {
			recentlyRunNodes.Clear();
		}

		public NodeRunner(bool verbose = false) : this() {
			this.verbose = verbose;
		}

		#endregion

		#region Public Methods

		#region Start

		public Promise Start(RunnableNode node) {
			Reset();

			queuedNodes.Enqueue(node);

			return StartPostProcess();
		}

		/// <summary> Start the runner, providing one or more starting nodes. </summary>
		public Promise Start(params RunnableNode[] nodes) {
			Reset();

			foreach (var node in nodes) {
				Print("Starting with node(s): " + node.GetType().ToString());
				queuedNodes.Enqueue(node);
			}

			return StartPostProcess();
		}

		/// <summary> Start the runner, providing one or more starting nodes. </summary>
		public Promise Start(IEnumerable<RunnableNode> nodes) {
			Reset();

			foreach (var node in nodes) {
				Print("Starting with node(s): " + node.GetType().ToString());
				queuedNodes.Enqueue(node);
			}

			return StartPostProcess();
		}

		protected Promise StartPostProcess() {

			promise = new Promise();

			QueueNextNodeOrEnd();

			runStatus = RunStatus.Running;

			return promise;
		}

		#endregion

		#region Stop

		/// <summary> Interrupts the process and resets it to a cleared state. </summary>
		public void StopAndReset() {

			// Children first, so a sub-runner is never left running against a parent that has already reset.
			// This is what makes most RunnableNode.Cancel overrides unnecessary: a node no longer has to find
			// and stop the runners it started, because the runner that owns them does it.
			StopSubRunners();

			var node = runStatus == RunStatus.Running ? runningNode : lastRunningNode;

			if (TryGetLastNodeEvent(runningNode, out var @event)) {
				@event.name = "Canceled";
				@event.timeStamp = Time.time;
			}
			else {
				@event = new NodeRunnerEvent() {
					name = "Canceled",
					node = runningNode,
					timeStamp = Time.time
				};
				recentlyRunNodes.Add(@event);
			}

			if (node != null) {
				node.Cancel(this);
			}
			if (runStatus == RunStatus.Running) {
				doPostRunLogic = false;
			}

			promise = null;
			runStatus = RunStatus.Stopped;
			Reset();
		}

		#endregion

		#region Resume

		/// <summary>
		/// Cause the runner to resume processing.
		/// This must be called after the runner has been suspended.
		/// </summary>
		public void Resume() {
			if (runStatus == RunStatus.Stopped) {
				return;
			}
			if (runStatus != RunStatus.Running) {
				QueueNextNodeOrEnd();
				runStatus = RunStatus.Running;
			}
			else {
				postCurrentNodeBehavior = PostNodeBehavior.Continue;
			}
		}

		/// <summary>
		/// Cause the runner to resume processing.
		/// This must be called after the runner has been suspended.
		/// Allows for a node to be 'inserted' and run.
		/// </summary>
		public void Resume(RunnableNode node) {
			if (runStatus == RunStatus.Stopped) {
				return;
			}
			if (node != null) {
				queuedNodes.Prepend(node);
			}
			if (runStatus != RunStatus.Running) {
				QueueNextNodeOrEnd();
				runStatus = RunStatus.Running;
			}
			else {
				postCurrentNodeBehavior = PostNodeBehavior.Continue;
			}
		}

		#endregion

		#region Node Manipulation

		/// <summary>
		/// Inserts a node to be run next. Assumes that the process is already running or will be resumed.
		/// The given node will not run until after the current node has finished processing.
		/// </summary>
		public void Prepend(RunnableNode node) {
			if (node != null) {
				queuedNodes.Prepend(node);
			}
		}

		public void Prepend(params RunnableNode[] nodes) {
			foreach (var node in nodes) {
				queuedNodes.Prepend(node);
			}
		}

		public void Prepend(IEnumerable<RunnableNode> nodes) {
			foreach (var node in nodes.Reverse()) {
				queuedNodes.Prepend(node);
			}
		}

		/// <summary>
		/// Add a node to be run. Assumes that the process is already running or will be resumed.
		/// The given node will not run until after all other nodes.
		/// </summary>
		public void Append(RunnableNode node) {
			if (node != null) {
				queuedNodes.Add(node);
			}
		}

		public void Append(params RunnableNode[] nodes) {
			queuedNodes.AddRange(nodes);
		}

		public void Append(IEnumerable<RunnableNode> nodes) {
			queuedNodes.AddRange(nodes);
		}

		public void Suspend() {
			postCurrentNodeBehavior = PostNodeBehavior.Suspend;
		}

		public bool TryGetLastNodeEvent(RunnableNode node, out NodeRunnerEvent @event) {
			@event = recentlyRunNodes.FirstOrDefault(x => x.node == node);
			return @event != null;
		}

		#endregion

		#endregion

		#region Private Processing Methods

		protected void Reset() {
			runningNode = null;
			lastRunningNode = null;

			queuedNodes.Clear();
			runStatus = RunStatus.Stopped;

			doPostRunLogic = false;

			postCurrentNodeBehavior = PostNodeBehavior.Continue;

		}

		public bool Update() {
			switch (runStatus) {

				case RunStatus.Running:
					if (runningNode != null) {
						postCurrentNodeBehavior = PostNodeBehavior.Continue;

						Print("Running Node: " + runningNode.GetType().ToString());

						if (TryGetLastNodeEvent(runningNode, out var @event)) {
							@event.name = "Ran";
							@event.timeStamp = Time.time;
						}
						else {
							@event = new NodeRunnerEvent() {
								name = "Ran",
								node = runningNode,
								timeStamp = Time.time
							};
							recentlyRunNodes.Add(@event);
						}

						runningNode.Run(this);

						lastRunningNode = runningNode;
						runningNode = null;


						switch (postCurrentNodeBehavior) {
							case PostNodeBehavior.Suspend:
								runStatus = RunStatus.Suspended;
								break;

							case PostNodeBehavior.Continue:
								QueueNextNodeOrEnd();
								break;

						}
					}

					if (runningNode == null) {
						// == Post Run Logic ==
						if (doPostRunLogic) {
							// End the Runner.
							Print("At end, invoking callback...");

							// store the callback
							var lastPromise = promise;

							// put the runner back in a fresh state
							Reset();

							// invoke the callback here
							lastPromise?.Resolve();
						}
						else {
							Print("Process suspending...");
						}
					}

					return true;

				default:
					return false;
			}
		}

		protected void QueueNextNodeOrEnd() {

			if (queuedNodes.Count > 0) {
				var nextNode = queuedNodes.Dequeue();

				runningNode = nextNode;
			}
			else {

				// let the runner end
				runningNode = null;

				doPostRunLogic = true;
			}
		}

		#endregion

		#region Debug and Logging

		protected void Print(string message, bool forceVerbose = false) {
			if (verbose || forceVerbose) {
				Debug.Log($"{message} ");
			}
		}

		#endregion
	}
}