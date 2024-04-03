using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {

	public abstract class VerbGraph : NodeGraph {

		[SerializeField]
		private List<InputNarramancerPort> inputs = new List<InputNarramancerPort>();
		public List<InputNarramancerPort> Inputs => inputs;
		public static string InputsFieldName => nameof(inputs);

		[SerializeField]
		private List<NarramancerPort> outputs = new List<NarramancerPort>();
		public List<NarramancerPort> Outputs => outputs;
		public static string OutputsFieldName => nameof(outputs);

		[SerializeField]
		private ReferenceList references = new ReferenceList();

		public void ValidatePorts() {
			var hashset = new HashSet<string>();

			foreach (var input in Inputs) {
				if (hashset.Contains(input.Id)) {
					input.GenerateNewId();
				}
				hashset.Add(input.Id);
			}

			foreach (var output in Outputs) {
				if (hashset.Contains(output.Id)) {
					output.GenerateNewId();
				}
				hashset.Add(output.Id);
			}
		}

		#region Get Node Convenience Methods

		public T GetNode<T>() where T : Node {
			foreach (var node in nodes) {
				if (typeof(T).IsAssignableFrom(node.GetType())) {
					return node as T;
				}
			}
			return null;
		}

		public Node GetNode(Type type) {
			foreach (var node in nodes) {
				if (type.IsAssignableFrom(node.GetType())) {
					return node;
				}
			}
			return null;
		}

		public IEnumerable<T> GetNodesOfType<T>() where T : Node {
			foreach (var node in nodes) {
				if (typeof(T).IsAssignableFrom(node.GetType())) {
					yield return node as T;
				}
			}
		}

		public IEnumerable<Node> GetNodesOfType(Type type) {
			foreach (var node in nodes) {
				if (type.IsAssignableFrom(node.GetType())) {
					yield return node;
				}
			}
		}

		public bool HasNode(Type nodeType) {
			foreach (var node in nodes) {
				if (nodeType.IsAssignableFrom(node.GetType())) {
					return true;
				}
			}
			return false;
		}

		public bool HasNode<T>() {
			foreach (var node in nodes) {
				if (typeof(T).IsAssignableFrom(node.GetType())) {
					return true;
				}
			}
			return false;
		}

		public bool TryGetNode<T>(out T resultNode) where T : Node {

			resultNode = GetNode<T>();
			return resultNode != null;
		}

		#endregion

		#region Output Node

		[NonSerialized] private OutputNode cachedOutputNode = null;

		public bool TryGetOutputNode(out OutputNode resultNode) {

			resultNode = GetOutputNode();
			return resultNode != null;
		}

		public OutputNode GetOutputNode() {
			if (cachedOutputNode != null) {
				return cachedOutputNode;
			}
			else
			if (TryGetNode<OutputNode>(out cachedOutputNode)) {
				return cachedOutputNode;
			}
			else {
				Debug.LogError($"IO Graph {this} not set or not setup properly.");
				return null;
			}
		}

		#endregion

		#region Input/Ouput

		public InputNarramancerPort AddInput<T>(string name = "value") {
			var newInput = new InputNarramancerPort();
			newInput.Type = typeof(T);
			newInput.Name = name;
			inputs.Add(newInput);
			return newInput;
		}

		public InputNarramancerPort AddInput(Type type, string name = "value") {
			var newInput = new InputNarramancerPort();
			newInput.Type = type;
			newInput.Name = name;
			inputs.Add(newInput);
			return newInput;
		}

		public InputNarramancerPort GetOrAddInput(Type type, string name = "value") {
			var inputPort = GetInput(type, name);
			if (inputPort != null) {
				return inputPort;
			}
			return AddInput(type, name);
		}

		public NarramancerPort AddOutput<T>(string name = "result") {
			var newOutput = new NarramancerPort();
			newOutput.Type = typeof(T);
			newOutput.Name = name;
			outputs.Add(newOutput);
			return newOutput;
		}

		public NarramancerPort AddOutput(Type type, string name = "result") {
			var newOutput = new NarramancerPort();
			newOutput.Type = type;
			newOutput.Name = name;
			outputs.Add(newOutput);
			return newOutput;
		}

		public NarramancerPort GetOrAddOutput(Type type, string name = "result") {
			var outputPort = GetOutput(name, type);
			if (outputPort != null) {
				return outputPort;
			}
			return AddOutput(type, name);
		}


		public bool HasInput<T>() {
			var type = typeof(T);
			return inputs.Any(input => input.Type == type);
		}

		public bool HasInput(Type type) {
			return inputs.Any(input => input.Type == type);
		}

		public bool HasOutput<T>() {
			var type = typeof(T);
			return outputs.Any(output => output.Type == type);
		}

		public bool HasOutput(Type type) {
			return outputs.Any(output => output.Type == type);
		}

		public InputNarramancerPort GetInput<T>() {
			return inputs.FirstOrDefault(input => input.Type == typeof(T));
		}

		public InputNarramancerPort GetInput(Type type) {
			return inputs.FirstOrDefault(input => input.Type == type);
		}

		public InputNarramancerPort GetInput<T>(string name) {
			return inputs.FirstOrDefault(input => input.Type == typeof(T) && input.Name.Equals(name));
		}

		public InputNarramancerPort GetInput(Type type, string name) {
			return inputs.FirstOrDefault(input => input.Type == type && input.Name.Equals(name));
		}

		public NarramancerPort GetOutput<T>() {
			return outputs.FirstOrDefault(output => output.Type == typeof(T));
		}

		public NarramancerPort GetOutput(Type type) {
			return outputs.FirstOrDefault(output => output.Type == type);
		}

		public NarramancerPort GetOutput<T>(string name) {
			return outputs.FirstOrDefault(output => output.Type == typeof(T) && output.Name.Equals(name));
		}

		public NarramancerPort GetOutput(string name, Type type) {
			return outputs.FirstOrDefault(output => output.Type == type && output.Name.Equals(name));
		}

		#endregion

	}
}