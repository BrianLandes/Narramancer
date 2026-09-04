using System;
using UnityEngine;

namespace Narramancer {

	/// <summary>
	/// What to do when a promise resolves, stored as <b>data</b> rather than as a delegate.
	/// <para>
	/// This replaces serializing a C# closure. A lambda's identity is compiler-generated — <c>PrintTextNode</c>
	/// has been saved under <c>&lt;&gt;c__DisplayClass22_0</c>, <c>1_0</c> and <c>19_0</c> across real saves,
	/// because the suffix is a method ordinal that moves when the source file is edited. Editing a node could
	/// therefore invalidate saved games, silently and with no warning. A named intention plus its arguments
	/// has no such problem: nothing in it is invented by the compiler.
	/// </para>
	/// <para>
	/// The set of kinds is deliberately small. The callbacks being replaced look general but are not — across
	/// the whole node library they are a handful of fixed intentions, nearly always "resume this runner".
	/// </para>
	/// </summary>
	[Serializable]
	public class Continuation {

		public enum Kind {
			/// <summary>Resume the runner where it suspended.</summary>
			ResumeRunner,

			/// <summary>Resume the runner, running <see cref="node"/> first — the choice callbacks.</summary>
			ResumeRunnerWithNode,

			/// <summary>Release a sub-runner the story no longer needs.</summary>
			ReleaseRunner,

			/// <summary>Set a flag on the runner's blackboard.</summary>
			SetBlackboardBool,

			/// <summary>Hand back to <see cref="RunnableNode.OnContinuation"/> — the escape hatch.</summary>
			NodeCallback,
		}

		[SerializeField]
		private Kind kind = Kind.ResumeRunner;

		/// <summary>
		/// Which runner this acts on, by its key in <c>StoryInstance.NodeRunners</c>. A key rather than a
		/// reference so the continuation stays plain data — and the runner it names may legitimately be gone
		/// by the time this runs, which is not an error.
		/// </summary>
		[SerializeField]
		private string runnerKey = null;

		/// <summary>The node to run first, for <see cref="Kind.ResumeRunnerWithNode"/>.</summary>
		[SerializeField]
		private RunnableNode node = null;

		/// <summary>The node whose <see cref="RunnableNode.OnContinuation"/> to call, for <see cref="Kind.NodeCallback"/>.</summary>
		[SerializeField]
		private RunnableNode callbackNode = null;

		/// <summary>A blackboard key, or an author-chosen callback id. Never a compiler-generated name.</summary>
		[SerializeField]
		private string key = null;

		[SerializeField]
		private bool boolValue = false;

		public Kind ContinuationKind => kind;
		public string RunnerKey => runnerKey;

		private Continuation() {
		}

		public static Continuation Resume(NodeRunner runner) =>
			new Continuation { kind = Kind.ResumeRunner, runnerKey = runner?.name };

		public static Continuation ResumeWith(NodeRunner runner, RunnableNode node) =>
			new Continuation { kind = Kind.ResumeRunnerWithNode, runnerKey = runner?.name, node = node };

		public static Continuation Release(NodeRunner runner) =>
			new Continuation { kind = Kind.ReleaseRunner, runnerKey = runner?.name };

		public static Continuation SetBool(NodeRunner runner, string key, bool value) =>
			new Continuation { kind = Kind.SetBlackboardBool, runnerKey = runner?.name, key = key, boolValue = value };

		/// <summary>
		/// Calls back into a node, identified by an id the node's author chooses. That id is the whole point:
		/// it is stable across edits, unlike the compiler-generated name of the lambda it replaces.
		/// </summary>
		public static Continuation NodeCallbackTo(RunnableNode node, string callbackId, NodeRunner runner) =>
			new Continuation { kind = Kind.NodeCallback, callbackNode = node, key = callbackId, runnerKey = runner?.name };

		public void Invoke() {
			var runner = ResolveRunner();

			switch (kind) {

				case Kind.ResumeRunner:
					runner?.Resume();
					break;

				case Kind.ResumeRunnerWithNode:
					if (runner != null) {
						if (node != null) {
							runner.Resume(node);
						}
						else {
							runner.Resume();
						}
					}
					break;

				case Kind.ReleaseRunner:
					if (runner != null) {
						NarramancerSingleton.Instance.ReleaseNodeRunner(runner);
					}
					break;

				case Kind.SetBlackboardBool:
					runner?.Blackboard.SetBool(key, boolValue);
					break;

				case Kind.NodeCallback:
					if (callbackNode != null) {
						callbackNode.OnContinuation(key, runner);
					}
					else {
						Debug.LogError($"A saved continuation named the callback '{key}', but the node it belongs to is missing from this project.");
					}
					break;
			}
		}

		/// <summary>
		/// The runner this names, or null if the story no longer has it.
		/// <para>
		/// A missing runner is <b>not</b> an error: a runner that was released, or a story loaded past the
		/// point this continuation mattered, both legitimately leave the key dangling. Every branch of
		/// <see cref="Invoke"/> is written to do nothing in that case, which is the same thing the old closure
		/// did by holding a reference to an object nobody else pointed at any more.
		/// </para>
		/// </summary>
		private NodeRunner ResolveRunner() {
			if (runnerKey.IsNullOrEmpty()) {
				return null;
			}
			var singleton = NarramancerSingleton.Instance;
			return singleton != null ? singleton.GetNodeRunner(runnerKey) : null;
		}
	}
}
