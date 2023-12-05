using System;
using System.Collections.Generic;
using UnityEngine;

namespace Narramancer {

	[Serializable]
	public class NarramancerPortWithAssignmentList {
		[SerializeField]
		public List<NarramancerPortWithAssignment> list = new List<NarramancerPortWithAssignment>();
	}

}
