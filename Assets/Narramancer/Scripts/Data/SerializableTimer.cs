using System;

namespace Narramancer {

	[Serializable]
	public class SerializableTimer {

		/// <summary>
		/// How much longer this timer has to run, counted down each frame.
		/// <para>
		/// A <b>remaining duration</b>, deliberately, not a <c>Time.time</c> deadline. <c>Time.time</c> is
		/// measured from when the game started, so it resets on relaunch: a deadline of "fire at t=104" saved
		/// in a session that had been running 100 seconds becomes "fire 104 seconds from now" after a load.
		/// Every saved wait was therefore delayed by however long the previous session had been running, and
		/// the later you saved the worse it got. A remaining duration means nothing has to be rebased on load.
		/// </para>
		/// </summary>
		public float secondsRemaining;

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
