using System;
using UnityEngine;

namespace Narramancer {

	[Serializable]
	public class InputNarramancerPort : NarramancerPort {

		[SerializeField]
		[Tooltip("Automatically creates a corresponding output for this input, allowing the value to be easily used again downstream.")]
		private bool passThrough = false;
		public static string PassThroughFieldName => nameof(passThrough);

		public bool PassThrough => passThrough;
	}
}