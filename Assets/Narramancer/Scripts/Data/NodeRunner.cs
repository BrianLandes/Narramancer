
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