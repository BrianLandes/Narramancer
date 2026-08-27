using System;

namespace Narramancer.Serialization {

	/// <summary>
	/// Thrown when serializing or deserializing fails: malformed JSON, an unresolvable type, an
	/// unsupported value. The save system deliberately fails loudly rather than producing a
	/// partially-populated graph — a save that refuses to load beats one that silently resumes in
	/// the wrong place.
	/// </summary>
	public sealed class SerializationException : Exception {

		public SerializationException(string message) : base(message) {
		}

		public SerializationException(string message, Exception inner) : base(message, inner) {
		}
	}
}
