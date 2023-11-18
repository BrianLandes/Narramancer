
using System;

namespace Narramancer {
	[Serializable]
	public class NodeRunnerEvent {
		public string name;
		public RunnableNode node;
		public float timeStamp;

	}
}