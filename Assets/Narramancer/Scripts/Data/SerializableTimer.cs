using System;

namespace Narramancer {

	[Serializable]
	public class SerializableTimer {

		public float timeStamp;
		public Promise promise;

		/// <summary>
		/// Who started this timer, so it can be cancelled when they stop. Optional — a timer with no owner
		/// simply cannot be cancelled early.
		/// <para>
		/// Without this a cancelled node left its timer running, and since the timer holds a
		/// <see cref="Continuation"/> naming a runner <b>by key</b>, and a released runner's name becomes
		/// available again, a stale timer could resume a completely different runner that happened to be given
		/// the same name later.
		/// </para>
		/// </summary>
		public string ownerKey;
	}
}
