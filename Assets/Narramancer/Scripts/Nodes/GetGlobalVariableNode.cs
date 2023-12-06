//using System.Linq;
//using UnityEngine;
//using XNode;

//namespace Narramancer {
//	[CreateNodeMenu("Variable/Get Global Variable")]
//	public class GetGlobalVariableNode : Node {

//		[SerializeField]
//		private string inputId = "";

//		public static string OUTPUT_PORT_NAME = "value";

//		public NarramancerPort GetVariable() {
//			if (inputId.IsNullOrEmpty()) {
//				return null;
//			}
//			return NarramancerSingleton.Instance.GlobalVariables.FirstOrDefault(x => x.Id.Equals(inputId));
//		}

//		public void SetInput(NarramancerPort graphPort) {
//			inputId = graphPort.Id;
//			ApplyChanges();
//		}

//		public void ApplyChanges() {

//			var input = GetVariable();

//			if (input == null) {
//				ClearDynamicPorts();
//				return;
//			}

//			var objectType = input.Type;

//			var outputPort = this.GetOrAddDynamicOutput(objectType, OUTPUT_PORT_NAME);

//			this.ClearDynamicPortsExcept(outputPort);
//		}


//		public override object GetValue(object context, NodePort port) {
//			if (!Application.isPlaying) {
//				return null;
//			}
//			var input = GetVariable();
//			if (input == null) {
//				return null;
//			}
//			var blackboard = NarramancerSingleton.Instance.StoryInstance.Blackboard;
//			var value = blackboard.Get(input.VariableKey, input.Type);
//			return value;
//		}
//	}
//}