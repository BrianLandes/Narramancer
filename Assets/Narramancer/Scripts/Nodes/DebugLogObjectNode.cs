
using UnityEngine;

namespace Narramancer {
	[NodeWidth(350)]
	public class DebugLogObjectNode : ChainedRunnableNode {

		const string INPORT_PORT = "Input Object";

		[SerializeField]
		private SerializableType type = new SerializableType();

		protected override void Init() {
			type.OnChanged -= UpdatePorts;
			type.OnChanged += UpdatePorts;
		}

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			var inputPort = GetPort(INPORT_PORT);

			var inputFromPort = inputPort.GetInputValue(runner.Blackboard);

			Debug.Log(inputFromPort);
		}

		public override void UpdatePorts() {

			if (type.Type != null) {
				var inputPort = this.GetOrAddDynamicInput(type.Type, INPORT_PORT);
				this.ClearDynamicPortsExcept(inputPort);
			}
			else {
				ClearDynamicPorts();
			}

			base.UpdatePorts();
		}

	}
}