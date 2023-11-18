﻿using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {

	[NodeWidth(400)]
	[CreateNodeMenu("Logic/Are Equal")]
	public class AreEqualNode : Node {

		[SerializeField]
		private SerializableType type = new SerializableType();

		private static string LEFT_OBJECT = "Left Object";
		private static string RIGHT_OBJECT = "Right Object";

		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
		[SerializeField]
		private bool areEqual = false;

		protected override void Init() {
			type.OnChanged -= UpdatePorts;
			type.OnChanged += UpdatePorts;
		}

		public override void UpdatePorts() {

			if (type.Type == null) {
				ClearDynamicPorts();
			}
			else {
				List<NodePort> existingPorts = new List<NodePort>();

				var leftInputPort = this.GetOrAddDynamicInput(type.Type, LEFT_OBJECT);
				existingPorts.Add(leftInputPort);

				var rightInputPort = this.GetOrAddDynamicInput(type.Type, RIGHT_OBJECT);
				existingPorts.Add(rightInputPort);

				this.ClearDynamicPortsExcept(existingPorts);
			}

			base.UpdatePorts();
		}


		public override object GetValue(object context, NodePort port) {
			if (type.Type == null) {
				return null;
			}
			switch (port.fieldName) {
				case nameof(areEqual):

					var leftPort = this.GetDynamicInput(type.Type, LEFT_OBJECT);
					var rightPort = this.GetDynamicInput(type.Type, RIGHT_OBJECT);

					var left = leftPort.GetInputValue(context);
					var right = rightPort.GetInputValue(context);

					return left == right;
			}
			return null;
		}
	}
}