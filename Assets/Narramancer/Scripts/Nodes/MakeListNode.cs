
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {

	public class MakeListNode : Node {

		[SerializeField]
		private SerializableType listType = new SerializableType();

		[SerializeField]
		private List<ScriptableObject> @objects = new List<ScriptableObject>();

		private const string RESULT = "Result";

		protected override void Init() {
			listType.OnChanged -= UpdatePorts;
			listType.OnChanged += UpdatePorts;
		}

		public override void UpdatePorts() {

			if (listType.Type == null) {
				ClearDynamicPorts();
			}
			else {
				var outputListPort = this.GetOrAddDynamicOutput(listType.TypeAsList, RESULT);
				this.ClearDynamicPortsExcept(outputListPort);
			}

			base.UpdatePorts();
		}

		public override object GetValue(INodeContext context, NodePort port) {
			if (listType.Type != null && port.fieldName.Equals(RESULT)) {

				var type = listType.Type;

				var result = @objects.Where(@object => @object.GetType() == type).ToList();
				return result;
			}
			return null;
		}

	}
}