using System;
using System.Collections.Generic;
using UnityEngine;

namespace Narramancer {

	[Serializable]
	public class ActionVerbList {

		[SerializeField]
		public List<ActionVerb> list = new List<ActionVerb>();
	}
}
