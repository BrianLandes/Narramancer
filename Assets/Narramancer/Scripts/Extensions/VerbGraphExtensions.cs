using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {
	public static class VerbGraphExtensions {

		public static void BuildIONodeGraphPorts(this Node node, VerbGraph nodeGraph) {

			List<NodePort> existingPorts = new List<NodePort>();

			foreach (var inputPort in nodeGraph.Inputs) {
				if (inputPort.Type == null) {
					continue;
				}

				var nodePort = node.GetOrAddDynamicInput(inputPort.Type, inputPort.Name);
				existingPorts.Add(nodePort);

				if (inputPort.PassThrough) {
					nodePort = node.GetOrAddDynamicOutput(inputPort.Type, inputPort.Name + "PassThrough");
					existingPorts.Add(nodePort);
				}
			}

			foreach (var outputPort in nodeGraph.Outputs) {
				if (outputPort.Type == null) {
					continue;
				}
				var nodePort = node.GetOrAddDynamicOutput(outputPort.Type, outputPort.Name);
				existingPorts.Add(nodePort);
			}

			foreach (var nodePort in node.DynamicPorts.Except(existingPorts).ToList()) {
				node.RemoveDynamicPort(nodePort);
			}
		}

		public static IEnumerable<NodePort> GetOrAddPortsForGraph(this Node node, VerbGraph nodeGraph, IEnumerable<NarramancerPort> skipPorts = null) {

			foreach (var inputPort in nodeGraph.Inputs) {
				if (inputPort.Type == null) {
					continue;
				}
				if (skipPorts != null && skipPorts.Contains(inputPort)) {
					continue;
				}
				var nodePort = node.GetOrAddDynamicInput(inputPort.Type, inputPort.Name);
				yield return nodePort;
			}

			foreach (var outputPort in nodeGraph.Outputs) {
				if (outputPort.Type == null) {
					continue;
				}
				if (skipPorts != null && skipPorts.Contains(outputPort)) {
					continue;
				}
				var nodePort = node.GetOrAddDynamicOutput(outputPort.Type, outputPort.Name);
				yield return nodePort;
			}
		}

		public static ReturnType RunForValue<ReturnType>(this VerbGraph nodeGraph, INodeContext context) {
			if (nodeGraph.TryGetOutputNode(out var outputNode)) {

				var resultOutput = nodeGraph.GetOutput<ReturnType>();
				if (resultOutput == null) {
					Debug.LogError("Graph does not have an output of type " + typeof(ReturnType).Name, nodeGraph);
					return default(ReturnType);
				}
				return (ReturnType)outputNode.GetValue(context, resultOutput);

			}
			else {
				Debug.LogError("Graph does not have an Output Node.", nodeGraph);
				return default(ReturnType);
			}
		}

		public static ReturnType RunForValue<ReturnType, T1>(this VerbGraph nodeGraph, INodeContext context, T1 inputValue) {
			if (nodeGraph.TryGetOutputNode(out var outputNode)) {

				var inputPort = nodeGraph.GetInput<T1>();
				if (inputPort == null) {
					Debug.LogError("Graph does not have an input of type " + typeof(T1).Name, nodeGraph);
					return default(ReturnType);
				}
				var blackboard = context as Blackboard;
				blackboard.Set(inputPort.VariableKey, inputValue);

				var resultOutput = nodeGraph.GetOutput<ReturnType>();
				if (resultOutput == null) {
					Debug.LogError("Graph does not have an output of type " + typeof(ReturnType).Name, nodeGraph);
					return default(ReturnType);
				}
				return (ReturnType)outputNode.GetValue(context, resultOutput);

			}
			else {
				Debug.LogError("Graph does not have an Output Node.", nodeGraph);
				return default(ReturnType);
			}
		}

		public static ReturnType RunForValue<ReturnType>(this VerbGraph nodeGraph, INodeContext context, Type inputType, object inputValue) {
			if (nodeGraph.TryGetOutputNode(out var outputNode)) {

				var inputPort = nodeGraph.GetInput(inputType);
				if (inputPort == null) {
					Debug.LogError("Graph does not have an input of type " + inputType.Name, nodeGraph);
					return default(ReturnType);
				}
				var blackboard = context as Blackboard;
				blackboard.Set(inputPort.VariableKey, inputValue);

				var resultOutput = nodeGraph.GetOutput<ReturnType>();
				if (resultOutput == null) {
					Debug.LogError("Graph does not have an output of type " + typeof(ReturnType).Name, nodeGraph);
					return default(ReturnType);
				}
				return (ReturnType)outputNode.GetValue(context, resultOutput);

			}
			else {
				Debug.LogError("Graph does not have an Output Node.", nodeGraph);
				return default(ReturnType);
			}
		}

		public static object RunForValue(this VerbGraph nodeGraph, INodeContext context, object inputValue) {
			if (nodeGraph.TryGetOutputNode(out var outputNode)) {

				var inputPort = nodeGraph.Inputs.FirstOrDefault();
				if (inputPort == null) {
					Debug.LogError("Graph does not have any inputs", nodeGraph);
					return null;
				}
				var blackboard = context as Blackboard;
				blackboard.Set(inputPort.VariableKey, inputValue);

				var resultOutput = nodeGraph.Outputs.FirstOrDefault();
				if (resultOutput == null) {
					Debug.LogError("Graph does not have any outputs", nodeGraph);
					return null;
				}
				return outputNode.GetValue(context, resultOutput);

			}
			else {
				Debug.LogError("Graph does not have an Output Node.", nodeGraph);
				return null;
			}
		}

		public static ReturnType RunForValue<ReturnType, T1, T2>(this VerbGraph nodeGraph, INodeContext context, T1 value1, T2 value2) {
			if (nodeGraph.TryGetOutputNode(out var outputNode)) {

				var blackboard = context as Blackboard;

				var firstInputPort = nodeGraph.GetInput<T1>();
				if (firstInputPort == null) {
					Debug.LogError("Graph does not have an input of type " + typeof(T1).Name, nodeGraph);
					return default(ReturnType);
				}
				blackboard.Set(firstInputPort.VariableKey, value1);

				var inputPort = nodeGraph.Inputs.Excluding(firstInputPort).FirstOrDefault(input => input.Type == typeof(T2));
				if (inputPort == null) {
					Debug.LogError("Graph does not have an input of type " + typeof(T2).Name, nodeGraph);
					return default(ReturnType);
				}
				blackboard.Set(inputPort.VariableKey, value2);

				var resultOutput = nodeGraph.GetOutput<ReturnType>();
				if (resultOutput == null) {
					Debug.LogError("Graph does not have an output of type " + typeof(ReturnType).Name, nodeGraph);
					return default(ReturnType);
				}
				return (ReturnType)outputNode.GetValue(context, resultOutput);
			}
			else {
				Debug.LogError("Graph does not have an Output Node.", nodeGraph);
				return default(ReturnType);
			}
		}

		public static List<ReturnType> RunForValueList<ReturnType, T1>(this VerbGraph nodeGraph, INodeContext context, T1 value1) {
			if (nodeGraph.TryGetOutputNode(out var outputNode)) {

				var blackboard = context as Blackboard;

				var firstInputPort = nodeGraph.GetInput<T1>();
				if (firstInputPort == null) {
					Debug.LogError("Graph does not have an input of type " + typeof(T1).Name, nodeGraph);
					return Enumerable.Empty<ReturnType>().ToList();
				}
				blackboard.Set(firstInputPort.VariableKey, value1);

				var resultOutput = nodeGraph.GetOutput<List<ReturnType>>();
				if (resultOutput == null) {
					Debug.LogError("Graph does not have an output of type " + typeof(ReturnType).Name, nodeGraph);
					return Enumerable.Empty<ReturnType>().ToList();
				}
				var value = outputNode.GetValue(context, resultOutput);
				var objectList = AssemblyUtilities.ToListOfObjects(value);
				var returnList = objectList.Cast<ReturnType>().ToList();
				return returnList;
			}
			else {
				Debug.LogError("Graph does not have an Output Node.", nodeGraph);
				return Enumerable.Empty<ReturnType>().ToList();
			}
		}

		public static List<ReturnType> RunForValueList<ReturnType, T1, T2>(this VerbGraph nodeGraph, INodeContext context, T1 value1, T2 value2) {
			if (nodeGraph.TryGetOutputNode(out var outputNode)) {

				var blackboard = context as Blackboard;

				var firstInputPort = nodeGraph.GetInput<T1>();
				if (firstInputPort == null) {
					Debug.LogError("Graph does not have an input of type " + typeof(T1).Name, nodeGraph);
					return Enumerable.Empty<ReturnType>().ToList();
				}
				blackboard.Set(firstInputPort.VariableKey, value1);

				var inputPort = nodeGraph.Inputs.Excluding(firstInputPort).FirstOrDefault(input => input.Type == typeof(T2));
				if (firstInputPort == null) {
					Debug.LogError("Graph does not have an input of type " + typeof(T2).Name, nodeGraph);
					return Enumerable.Empty<ReturnType>().ToList();
				}
				blackboard.Set(inputPort.VariableKey, value2);

				var resultOutput = nodeGraph.GetOutput<List<ReturnType>>();
				if (resultOutput == null) {
					Debug.LogError("Graph does not have an output of type " + typeof(ReturnType).Name, nodeGraph);
					return Enumerable.Empty<ReturnType>().ToList();
				}
				var value = outputNode.GetValue(context, resultOutput);
				var objectList = AssemblyUtilities.ToListOfObjects(value);
				var returnList = objectList.Cast<ReturnType>().ToList();
				return returnList;
			}
			else {
				Debug.LogError("Graph does not have an Output Node.", nodeGraph);
				return Enumerable.Empty<ReturnType>().ToList();
			}
		}

		public static bool HasMatchingNodeInputsForGraphOutputs(this Node node, VerbGraph graph) {
			if (graph.Outputs.Count != node.DynamicInputs.Count()) {
				return false;
			}

			bool NodeHasGraphPort(NarramancerPort port) {
				var nodePort = node.GetInputPort(port.Name);
				if (nodePort == null) {
					return false;
				}
				if (nodePort.ValueType != port.Type) {
					return false;
				}
				return true;
			}

			return graph.Outputs.Any(graphOutput => !NodeHasGraphPort(graphOutput));
		}

		public static bool HasMatchingNodeOutputsGraphInputs(this Node node, VerbGraph graph) {
			if (graph.Inputs.Count != node.DynamicOutputs.Count()) {
				return false;
			}

			bool NodeHasGraphPort(NarramancerPort port) {
				var nodePort = node.GetOutputPort(port.Name);
				if (nodePort == null) {
					return false;
				}
				if (nodePort.ValueType != port.Type) {
					return false;
				}
				return true;
			}

			return graph.Inputs.Any(graphInput => !NodeHasGraphPort(graphInput));
		}

		public static bool HasMatchingNodeOutputsAndInputsForGraphInputsAndOutputs(this Node node, VerbGraph graph) {
			return HasMatchingNodeInputsForGraphOutputs(node, graph) && HasMatchingNodeOutputsGraphInputs(node, graph);
		}

		public static bool HasMatchingNodeOutputsForGraphOutputs(this Node node, VerbGraph graph) {
			if (graph.Outputs.Count != node.DynamicOutputs.Count()) {
				return false;
			}

			bool NodeHasGraphPort(NarramancerPort port) {
				var nodePort = node.GetOutputPort(port.Name);
				if (nodePort == null) {
					return false;
				}
				if (nodePort.ValueType != port.Type) {
					return false;
				}
				return true;
			}

			return graph.Outputs.Any(graphOutput => !NodeHasGraphPort(graphOutput));
		}

		public static bool HasMatchingNodeInputsForGraphInputs(this Node node, VerbGraph graph) {
			if (graph.Inputs.Count != node.DynamicInputs.Count()) {
				return false;
			}

			bool NodeHasGraphPort(NarramancerPort port) {
				var nodePort = node.GetInputPort(port.Name);
				if (nodePort == null) {
					return false;
				}
				if (nodePort.ValueType != port.Type) {
					return false;
				}
				return true;
			}

			return graph.Inputs.Any(graphInput => !NodeHasGraphPort(graphInput));
		}

		public static bool HasMatchingNodeInputsAndOutputsForGraphInputsAndOutputs(this Node node, VerbGraph graph) {
			return HasMatchingNodeOutputsForGraphOutputs(node, graph) && HasMatchingNodeInputsForGraphInputs(node, graph);
		}

		public static void EnsurePortsHaveUniqueIds<T>(this List<T> variables) where T : NarramancerPort {
			var hashset = new HashSet<string>();

			foreach (var variable in variables) {
				if (variable.Id.IsNullOrEmpty() || hashset.Contains(variable.Id)) {
					variable.GenerateNewId();
				}
				hashset.Add(variable.Id);
			}
		}

	}
}