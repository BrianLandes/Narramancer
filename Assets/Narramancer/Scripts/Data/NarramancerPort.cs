
using System;
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	[Serializable]
	public class NarramancerPort {

		[SerializeField]
		private SerializableType type = new SerializableType();
		public static string TypeFieldName => nameof(type);

		[SerializeField]
		private string name = "value";
		public static string NameFieldName => nameof(name);

		[SerializeField]
		private string id;
		public static string IdFieldName => nameof(id);

		public NarramancerPort() {
			GenerateNewId();
		}

		public string VariableKey {
			get {
				return $"{ToString()} {id}";
			}
		}

		public Type Type { get => type.Type; set => type.Type = value; }
		public string Name { get => name; set => name = value; }
		public string Id => id;

		public void GenerateNewId() {
			id = Guid.NewGuid().ToString();
		}

		public override string ToString() {
			return $"{name} ({type.TypeName()})";
		}

		public void AssignValueFromNodePort(INodeContext context, NodePort nodePort) {
			var blackboard = context as Blackboard;

			var inputValue = nodePort.GetInputValue(context);
			blackboard.Set(VariableKey, inputValue);

		}

	}
}