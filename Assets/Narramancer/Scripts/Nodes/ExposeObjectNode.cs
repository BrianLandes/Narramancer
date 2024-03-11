using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using XNode;

namespace Narramancer {
	public class ExposeObjectNode : Node {

		[SerializeField]
		private SerializableType type = new SerializableType();

		[SerializeField, HideInInspector]
		private bool showAllFields = true;

		private const string ELEMENT = "Element";

		protected override void Init() {
			type.OnChanged -= UpdatePorts;
			type.OnChanged += UpdatePorts;
			showAllFields = true; // ensures that duplicating nodes works the way its supposed to
		}

		public override void UpdatePorts() {

			if ( type.Type == null) {
				ClearDynamicPorts();
			}
			else {
				var keepPorts = new List<NodePort>();

				var inputPort = this.GetOrAddDynamicInput(type.Type, ELEMENT);
				keepPorts.Add(inputPort);

				var fields = type.Type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

				foreach( var field in fields) {
					if (showAllFields) {
						var outputPort = this.GetOrAddDynamicOutput(field.FieldType, field.Name);
						keepPorts.Add(outputPort);
					}
					else {
						var outputPort = GetOutputPort(field.Name, field.FieldType);
						if (outputPort != null && outputPort.IsConnected) {
							keepPorts.Add(outputPort);
						}
					}
				}

				var methods = type.Type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

				foreach (var method in methods) {
					if (method.GetParameters().Any() || method.IsGenericMethod || method.ReturnType == typeof(void) ) {
						continue;
					}
					
					if (showAllFields) {
						var outputPort = this.GetOrAddDynamicOutput(method.ReturnType, method.Name);
						keepPorts.Add(outputPort);
					}
					else {
						var outputPort = GetOutputPort(method.Name, method.ReturnType);
						if (outputPort != null && outputPort.IsConnected) {
							keepPorts.Add(outputPort);
						}
					}
				}


				this.ClearDynamicPortsExcept(keepPorts);
			}

			base.UpdatePorts();
		}

		public override object GetValue(INodeContext context, NodePort port) {
			if ( Application.isPlaying) {

				var elementPort = GetInputPort(ELEMENT);
				var targetObject = elementPort.GetInputValue(context);

				var fields = type.Type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

				foreach (var field in fields) {
					if ( port.fieldName.Equals(field.Name, System.StringComparison.Ordinal)) {
						return field.GetValue(targetObject);
					}
				}

				var methods = type.Type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);

				foreach (var method in methods) {
					if (method.GetParameters().Any() || method.IsGenericMethod || method.ReturnType == typeof(void)) {
						continue;
					}
					if (port.fieldName.Equals(method.Name, System.StringComparison.Ordinal)) {
						return method.Invoke(targetObject, null);
					}
				}
			}
			return null;
		}
	}
}