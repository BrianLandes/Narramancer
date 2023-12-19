using UnityEngine;
using UnityEngine.Serialization;

namespace Narramancer {

	public abstract class AdjectiveScriptableObject : ScriptableObject {

		[SerializeField, FormerlySerializedAs("stringName")]
		ToggleableString overrideName = new ToggleableString(false);
		public ToggleableString OverrideName => overrideName;
	}
}