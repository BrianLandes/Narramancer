using System;
using System.Collections.Generic;
using UnityEngine;

namespace Narramancer {

	[Serializable]
	public class NounScriptableObjectList {
		[SerializeField]
		public List<NounScriptableObject> list = new List<NounScriptableObject>();
	}
}
