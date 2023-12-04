
//using System.Collections.Generic;
//using System.Linq;
//using UnityEngine;
//using XNode;

//namespace Narramancer {
//	[CreateNodeMenu("Variable/Set Global Variable")]
//	public class SetGlobalVariableNode : ChainedRunnableNode {

//		[SerializeField]
//		private string variableId = "";

//		public static string PORT_NAME = "value";

//		public override void Run(NodeRunner runner) {
//			base.Run(runner);

//			var outputPort = GetOutputPort();

//			if (outputPort == null) {
//				return;
//			}

//			var outputNodePort = GetInputPort(PORT_NAME);
//			if (outputNodePort == null) {
//				return;
//			}
//			var inputValue = outputNodePort.GetInputValue(runner.Blackboard);
//			NarramancerSingleton.Instance.StoryInstance.Blackboard.Set(outputPort.VariableKey, inputValue);

//		}

//		private NarramancerPort GetOutputPort() {
//			var verbGraph = graph as VerbGraph;
//			if (verbGraph == null) {
//				return null;
//			}
//			if (variableId.IsNullOrEmpty()) {
//				return null;
//			}
//			return NarramancerSingleton.Instance.GlobalVariables.FirstOrDefault(x => x.Id.Equals(variableId));
//		}

//		public void SetOutput(NarramancerPort outputPort) {
//			variableId = outputPort.Id;
//			ApplyChanges();
//		}

//		public void ApplyChanges() {

//			var output = GetOutputPort();

//			if (output == null) {
//				ClearDynamicPorts();
//				return;
//			}

//			var nodePorts = new List<NodePort>();

//			var objectType = output.Type;

//			var outputPort = this.GetOrAddDynamicInput(objectType, PORT_NAME);
//			nodePorts.Add(outputPort);

//			this.ClearDynamicPortsExcept(nodePorts);

//		}

//	}
//}