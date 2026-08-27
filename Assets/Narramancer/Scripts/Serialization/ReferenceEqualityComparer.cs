using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Narramancer.Serialization {

	/// <summary>
	/// Compares objects by reference identity rather than <c>Equals</c>, so the writer can tell when it
	/// has already written a given instance and emit a <c>$ref</c> instead of duplicating it.
	/// <para>
	/// This must not use <c>Equals</c>: <see cref="NounUID"/> and the dictionary types compare by value,
	/// and two equal-but-distinct instances have to stay distinct in a save.
	/// </para>
	/// </summary>
	internal sealed class ReferenceEqualityComparer : IEqualityComparer<object> {

		public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();

		bool IEqualityComparer<object>.Equals(object x, object y) => ReferenceEquals(x, y);

		int IEqualityComparer<object>.GetHashCode(object obj) => RuntimeHelpers.GetHashCode(obj);
	}
}
