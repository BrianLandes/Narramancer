using System.Collections.Generic;
using XNode;

namespace Narramancer {
	public interface IListTypeNode {

		SerializableType ListType { get; }

		IEnumerable<NodePort> DynamicInputs { get; }
	}
}
