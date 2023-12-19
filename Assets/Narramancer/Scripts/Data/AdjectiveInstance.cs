using System;
using UnityEngine;

namespace Narramancer {

	[Serializable]
	public abstract class AdjectiveInstance<T> : AdjectiveInstance where T : AdjectiveScriptableObject {

		/// <inheritdoc cref="AdjectiveInstance.adjective"/>
		public T Adjective => adjective as T;

		public AdjectiveInstance(T predefinedNoun) : base(predefinedNoun) {
		}

		// allow implicit conversion from our RuntimeContent type to the PredefinedContent type
		public static implicit operator T(AdjectiveInstance<T> runtimeNounInstance) => runtimeNounInstance.Adjective;
	}


	[Serializable]
	public abstract class AdjectiveInstance {

		/// <summary>
		/// The ScriptableObject this instance is based on.
		/// </summary>
		[SerializeField]
		protected AdjectiveScriptableObject adjective;
		public static string AdjectiveFieldName => nameof(adjective);

		public AdjectiveInstance(AdjectiveScriptableObject predefinedNoun) {
			this.adjective = predefinedNoun;
		}

		public override string ToString() {
			if (adjective == null) {
				return base.ToString();
			}
			if (adjective.OverrideName.activated) {
				return adjective.OverrideName.value;
			}
			return adjective.name;
		}
	}

}