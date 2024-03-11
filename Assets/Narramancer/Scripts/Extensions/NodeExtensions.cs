using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using XNode;
using static XNode.Node;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Narramancer {

	public static class NodeExtensions {

		public static NodePort GetDynamicInput(this Node node, Type type, string name) {
			return node.DynamicInputs.FirstOrDefault(x => x.ValueType == type && x.fieldName.Equals(name));
		}

		public static NodePort GetDynamicInput(this Node node, string name) {
			return node.DynamicInputs.FirstOrDefault(x => x.fieldName.Equals(name));
		}

		public static NodePort GetDynamicOutput(this Node node, Type type, string name) {
			return node.DynamicOutputs.FirstOrDefault(x => x.ValueType == type && x.fieldName.Equals(name));
		}

		public static NodePort GetDynamicOutput(this Node node, string name) {
			return node.DynamicOutputs.FirstOrDefault(x => x.fieldName.Equals(name));
		}

		public static NodePort GetOrAddDynamicInput(this Node node, Type type, string name, ConnectionType connectionType = ConnectionType.Override) {
			var existingPort = node.GetDynamicInput(type, name);
			if (existingPort != null) {
				return existingPort;
			}
			// There's a chance that a port exists of the given name but does not match the type
			// Find and remove it if that's the case
			existingPort = node.GetDynamicInput(name);
			if (existingPort != null) {
				node.RemoveDynamicPort(existingPort);
			}
			var port = node.AddDynamicInput(type, connectionType, TypeConstraint.Inherited, name);
			return port;
		}

		public static NodePort GetOrAddDynamicOutput(this Node node, Type type, string name, bool sameLine = false, bool hideLabel = false) {
			var existingPort = node.GetDynamicOutput(type, name);
			if (existingPort != null) {
				return existingPort;
			}
			// There's a chance that a port exists of the given name but does not match the type
			// Find and remove it if that's the case
			existingPort = node.GetDynamicOutput(name);
			if (existingPort != null) {
				node.RemoveDynamicPort(existingPort);
			}
			var port = node.AddDynamicOutput(type, ConnectionType.Multiple, TypeConstraint.Inherited, name, sameLine, hideLabel);
			return port;
		}

		public static void ClearDynamicPortsExcept(this Node node, NodePort excludedNodePort) {
			foreach (var nodePort in node.DynamicPorts) {
				if (nodePort == excludedNodePort) {
					continue;
				}
				node.RemoveDynamicPort(nodePort);
			}
		}

		public static void ClearDynamicPortsExcept(this Node node, IEnumerable<NodePort> nodePorts) {
			foreach (var nodePort in node.DynamicPorts.Except(nodePorts).ToList()) {
				node.RemoveDynamicPort(nodePort);
			}
		}

		public static List<object> GetInputValueObjectList(this NodePort nodePort, INodeContext context) {
			var value = nodePort.GetInputValue(context);
			return AssemblyUtilities.ToListOfObjects(value);
		}

		public static IEnumerable<NodePort> GetConnections(this NodePort nodePort) {
			for (int i = 0; i < nodePort.ConnectionCount; i++) {
				yield return nodePort.GetConnection(i);
			}
		}

		public static List<T> GetInputValueList<T>(this Node node, INodeContext context, string portName) {
			var result = node.GetInputValue<List<T>>(context, portName);
			if (result == null) {
				var inputNode = node.GetInputPort(portName);
				if (!inputNode.IsConnected) {
					return Enumerable.Empty<T>().ToList();
				}
				result = inputNode
					.GetInputValueObjectList(context)
					.Cast<T>()
					.ToList();
			}
			return result;
		}

		public static object GetInputValue(this Node node, INodeContext context, string fieldName) {
			var nodePort = node.GetInputPort(fieldName);
			return nodePort.GetInputValue(context);
		}

		public static Node AddNode<T>(this NodeGraph nodeGraph, Vector2 position) where T : Node {
			var node = nodeGraph.AddNode<T>();
			node.position = position;
			if (node.name == null || node.name.Trim() == "") {
				node.name = NodeDefaultName(typeof(T));
			}
#if UNITY_EDITOR
			if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(nodeGraph))) {
				AssetDatabase.AddObjectToAsset(node, nodeGraph);
			}
#endif

			return node;
		}

		public static string NodeDefaultName(Type type) {
			string typeName = type.Name;
			// Automatically remove redundant 'Node' postfix
			if (typeName.EndsWith("Node"))
				typeName = typeName.Substring(0, typeName.LastIndexOf("Node"));
#if UNITY_EDITOR
			typeName = ObjectNames.NicifyVariableName(typeName);
#endif
			return typeName;
		}


	}
}