using System;
namespace Narramancer {
	/// <summary>
	/// When applied to fields on a class that derives from <see cref="ISerializableMonoBehaviour"/>, causes those fields to be serialized and deserialized on save and load.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public class SerializeMonoBehaviourFieldAttribute : Attribute {

	}
}