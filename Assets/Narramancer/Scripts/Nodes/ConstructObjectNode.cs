using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using XNode;

namespace Narramancer {
	public class ConstructObjectNode : Node {

		[SerializeField]
		private SerializableType type = new SerializableType();

		private const string ELEMENT = "Element";

		protected override void Init() {
			type.OnChanged -= UpdatePorts;
			type.OnChanged += UpdatePorts;
		}

		public override void UpdatePorts() {

			if ( type.Type == null) {
				ClearDynamicPorts();
			}
			else {
				var keepPorts = new List<NodePort>();
				var fields = type.Type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

				foreach( var field in fields) {
					var outputPort = this.GetOrAddDynamicInput(field.FieldType, field.Name);
					keepPorts.Add(outputPort);
				}

				var inputPort = this.GetOrAddDynamicOutput(type.Type, ELEMENT);
				keepPorts.Add(inputPort);


				this.ClearDynamicPortsExcept(keepPorts);
			}

			base.UpdatePorts();
		}

		public override object GetValue(INodeContext context, NodePort port) {
			if ( Application.isPlaying) {

				var result = Activator.CreateInstance(type.Type);

				var fields = type.Type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

				foreach (var field in fields) {
					var fieldInputPort = GetInputPort(field.Name);
					var fieldValue = fieldInputPort.GetInputValue(context);
					if (fieldValue != null) {
						field.SetValue(result, fieldValue);
					}
				}

				return result;
			}
			return null;
		}
	}
}