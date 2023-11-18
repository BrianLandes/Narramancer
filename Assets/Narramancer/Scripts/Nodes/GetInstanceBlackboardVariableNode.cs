﻿using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("Variable/Get Instance Variable")]
	public class GetInstanceBlackboardVariableNode : AbstractInstanceInputNode {

		[SerializeField]
		private SerializableType valueType = new SerializableType();

		[SerializeField]
		string key = "value";

		protected override void Init() {
			valueType.OnChanged -= RebuildPorts;
			valueType.OnChanged += RebuildPorts;
		}

		private void RebuildPorts() {

			if (valueType.Type == null) {
				ClearDynamicPorts();
				return;
			}

			var outputValuePort = this.GetOrAddDynamicOutput(valueType.Type, "value", true, true);
			this.ClearDynamicPortsExcept(outputValuePort);

		}


		public override object GetValue(object context, NodePort port) {
			if (!Application.isPlaying) {
				return null;
			}

			var instance = GetInstance(context);
			var value = instance.Blackboard.Get(key, port.ValueType);
			return value;
		}
	}
}