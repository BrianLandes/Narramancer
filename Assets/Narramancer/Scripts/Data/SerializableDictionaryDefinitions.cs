
using Narramancer.SerializableDictionary;
using System;
using UnityEngine;

namespace Narramancer {

	[Serializable]
	public class StringPromiseDictionary : SerializableDictionary<string, Promise> { }

	[Serializable]
	public class StringNodeRunnerDictionary : SerializableDictionary<string, NodeRunner> { }

	[Serializable]
	public class FlagIntDictionary : SerializableDictionary<Flag, int> { }

	[Serializable]
	public class PropertyNounPropertyInstanceDictionary : SerializableDictionary<PropertyScriptableObject, PropertyInstance> { }

	[Serializable]
	public class StatNounStatInstanceDictionary : SerializableDictionary<StatScriptableObject, StatInstance> { }

	[Serializable]
	public class StringObjectDictionary : SerializableDictionary<string, object> { }

	[Serializable]
	public class StringUnityObjectDictionary : SerializableDictionary<string, UnityEngine.Object> { }

	[Serializable]
	public class StringComponentDictionary : SerializableDictionary<string, Component> { }

	[Serializable]
	public class StringGameObjectDictionary : SerializableDictionary<string, GameObject> { }

	[Serializable]
	public class StringStringDictionary : SerializableDictionary<string, string> { }

	[Serializable]
	public class StringFloatDictionary : SerializableDictionary<string, float> { }

	[Serializable]
	public class StringBoolDictionary : SerializableDictionary<string, bool> { }

	[Serializable]
	public class StringIntDictionary : SerializableDictionary<string, int> { }

	[Serializable]
	public class PrimitivePrimitiveDictionary : SerializableDictionary<SerializablePrimitive, SerializablePrimitive> { }


}