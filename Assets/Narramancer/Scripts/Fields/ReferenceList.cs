using System;
using System.Collections.Generic;
using UnityEngine;

namespace Narramancer {

	/// <summary>
	/// An Editor-only list of Unity Objects that reference the attached Asset. <br/>
	/// Allows for a quick overview of how assets are connected and reference each other.
	/// </summary>
	[Serializable]
	public class ReferenceList {
#if UNITY_EDITOR
		[SerializeField]
		public List<UnityEngine.Object> objects = new List<UnityEngine.Object>();
#endif
	}
}