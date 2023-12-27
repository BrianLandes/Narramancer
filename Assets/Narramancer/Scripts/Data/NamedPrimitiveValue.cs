using System;
using System.Collections.Generic;
using UnityEngine;

namespace Narramancer {

	[Serializable]
	public class NamedPrimitiveValueList {
		public List<NamedPrimitiveValue> list = new List<NamedPrimitiveValue>();
	}

	[Serializable]
	public class NamedPrimitiveValue {
		[SerializeField]
		public string name = string.Empty;

		[SerializeField]
		public SerializablePrimitive value = new SerializablePrimitive();
	}
}
