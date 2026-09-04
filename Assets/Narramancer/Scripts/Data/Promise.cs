using System;
using System.Collections.Generic;
using UnityEngine;

namespace Narramancer {

	/// <summary>
	/// Something that will finish later, and what should happen when it does.
	/// <para>
	/// Callbacks come in two kinds, and choosing between them is the whole design:
	/// </para>
	/// <list type="bullet">
	/// <item><description>
	/// <see cref="WhenDone(Continuation)"/> — stored as <b>data</b> and saved. Use it for anything that must
	/// still happen after the player loads a save. Everything reachable from a suspended verb is in this
	/// category.
	/// </description></item>
	/// <item><description>
	/// <see cref="WhenDone(Action)"/> — a plain delegate, <b>not</b> saved. Use it for UI glue, editor
	/// tooling, anything whose lifetime is the current frame.
	/// </description></item>
	/// </list>
	/// <para>
	/// The rule: <b>if this promise can still be pending when the player saves, the callback must be a
	/// <see cref="Continuation"/>.</b>
	/// </para>
	/// <para>
	/// Delegates used to be the only option, serialized by reflecting the compiler-generated closure class.
	/// That embedded a method ordinal in the save, so editing a node's source file could silently invalidate
	/// existing saved games — see docs/CONTINUATION_DESIGN.md.
	/// </para>
	/// </summary>
	[Serializable]
	public class Promise {

		[SerializeField]
		private List<Continuation> doneCallbacks = null;

		/// <summary>
		/// Callbacks that deliberately do not survive a save. Dropped on load, which is the intent: a promise
		/// restored from a save has no live UI still listening to it.
		/// </summary>
		[NonSerialized]
		private Action transientDoneCallbacks = null;

		public bool Resolved { get; private set; } = false;

		public void Reset() {
			Resolved = false;
			doneCallbacks?.Clear();
			transientDoneCallbacks = null;
		}

		/// <summary>Runs <paramref name="continuation"/> when this resolves, surviving a save.</summary>
		public Promise WhenDone(Continuation continuation) {
			if (continuation == null) {
				return this;
			}

			if (Resolved) {
				continuation.Invoke();
			}
			else {
				doneCallbacks = doneCallbacks ?? new List<Continuation>();
				doneCallbacks.Add(continuation);
			}
			return this;
		}

		/// <summary>
		/// Runs <paramref name="callback"/> when this resolves, <b>only within this session</b>. It is not
		/// saved, so a promise that is still pending at save time will not run it after a load.
		/// </summary>
		public Promise WhenDoneThisSession(Action callback) {
			if (callback == null) {
				return this;
			}

			if (Resolved) {
				callback.Invoke();
			}
			else {
				transientDoneCallbacks += callback;
			}
			return this;
		}

		public void Resolve() {
			// Snapshot and clear before invoking: a callback is allowed to start something that adds another
			// callback to this same promise, and that one belongs to the next resolve, not this one.
			var continuations = doneCallbacks;
			var transient = transientDoneCallbacks;
			doneCallbacks = null;
			transientDoneCallbacks = null;
			Resolved = true;

			if (continuations != null) {
				foreach (var continuation in continuations) {
					continuation.Invoke();
				}
			}

			transient?.Invoke();
		}
	}
}
