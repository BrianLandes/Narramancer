
using Narramancer.SerializableActionHelper;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Narramancer {

	[Serializable]
	public class Promise {

		[SerializeField]
		private List<SerializableAction> doneCallbacks;

		public bool Resolved { get; private set; } = false;

		public void Reset() {
			Resolved = false;
			doneCallbacks?.Clear();
		}

		public Promise WhenDone(Action callback) {

			if (Resolved) {
				callback?.Invoke();
			}
			else {

				var serializedCallback = new SerializableAction(callback);

				doneCallbacks = doneCallbacks != null ? doneCallbacks : new List<SerializableAction>();
				doneCallbacks.Add(serializedCallback);

			}

			return this;
		}

		public void Resolve() {
			if (doneCallbacks != null) {

				foreach (var callback in doneCallbacks) {
					callback.Action.Invoke();
				}

			}
			Resolved = true;
		}
	}
}