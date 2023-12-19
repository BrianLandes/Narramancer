using System;
using UnityEngine;

namespace Narramancer {
	[Serializable]
	public class NamedPrimitiveValue {
		[SerializeField]
		public string name = string.Empty;

		[SerializeField]
		public SerializablePrimitive value = new SerializablePrimitive();
	}
}
