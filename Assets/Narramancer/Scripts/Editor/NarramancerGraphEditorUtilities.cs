
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XNode;
using XNodeEditor;

namespace Narramancer {
	public static class NarramancerGraphEditorUtilities {


		public static void Collapse(this NodeGraph graph, IEnumerable<Node> nodes) {
			Undo.RecordObject(graph, "Collapse Nodes");

			var name = $"{graph.name} (SubGraph)";
			VerbGraph newGraph;
			var runnableNodes = nodes.Where(node => node is RunnableNode).Cast<RunnableNode>().ToArray();
			if (runnableNodes.Any()) {
				newGraph = PseudoEditorUtilities.CreateAndAddChild<ActionVerb>(graph, name);
			}
			else {
				newGraph = PseudoEditorUtilities.CreateAndAddChild<ValueVerb>(graph, name);
			}
			var actionVerb = newGraph as ActionVerb;

			var substitutes = new Dictionary<Node, Node>();

			var centerPoint = nodes.First().position;

			foreach (var selectedNode in nodes) {
				Node node = newGraph.CopyNode(selectedNode);
				node.name = selectedNode.name;
				node.position = selectedNode.position - centerPoint;

				Undo.RegisterCreatedObjectUndo(node, "Duplicate Node");
				if (PseudoEditorUtilities.IsObjectAnAsset(newGraph)) {
					AssetDatabase.AddObjectToAsset(node, newGraph);
				}

				substitutes.Add(selectedNode, node);
			}

			// Walk through the selected nodes again, recreate connections, using the new nodes
			foreach (var selectedNode in nodes) {
				foreach (var port in selectedNode.Ports) {
					for (int c = 0; c < port.ConnectionCount; c++) {
						NodePort inputPort = port.direction == NodePort.IO.Input ? port : port.GetConnection(c);
						NodePort outputPort = port.direction == NodePort.IO.Output ? port : port.GetConnection(c);

						Node newNodeIn, newNodeOut;
						if (substitutes.TryGetValue(inputPort.node, out newNodeIn) &&
								substitutes.TryGetValue(outputPort.node, out newNodeOut)) {
							newNodeIn.UpdatePorts();
							newNodeOut.UpdatePorts();
							inputPort = newNodeIn.GetInputPort(inputPort.fieldName);
							outputPort = newNodeOut.GetOutputPort(outputPort.fieldName);
						}
						if (!inputPort.IsConnectedTo(outputPort)) {
							inputPort.Connect(outputPort);
						}
					}
				}
			}

			// find the last runnable node in the chain

			var connectedRunnableNodes = new List<List<RunnableNode>>();

			foreach (var runnableNode in runnableNodes) {

				if (connectedRunnableNodes.Any(list => list.Contains(runnableNode))) {
					continue;
				}

				var bucket = new List<RunnableNode>();
				connectedRunnableNodes.Add(bucket);

				bucket.Add(runnableNode);

				var node = runnableNode.GetConnectedInputRunnableNodes().FirstOrDefault();
				while (node != null && nodes.Contains(node)) {
					bucket.Add(node);
					node = node.GetConnectedInputRunnableNodes().FirstOrDefault();
				}

				if (runnableNode is ChainedRunnableNode chainedRunnableNode && chainedRunnableNode.TryGetNextNode(out node)) {

					while (node != null && nodes.Contains(node)) {
						bucket.Add(node);
						if (node is ChainedRunnableNode c) {
							node = c.GetNextNode();
						}
						else {
							node = null;
						}
					}
				}
			}

			var biggestChain = connectedRunnableNodes.OrderByDescending(list => list.Count).FirstOrDefault();

			var lastRunnableNode = biggestChain?.FirstOrDefault(node => node is ChainedRunnableNode chainedRunnable && chainedRunnable.GetNextNode() == null);

			if (lastRunnableNode != null) {
				lastRunnableNode = substitutes[lastRunnableNode] as RunnableNode;
			}

			var firstRunnableNode = biggestChain?.FirstOrDefault(node => {
				var inputs = node.GetConnectedInputRunnableNodes();
				return inputs.Count == 0 || inputs.Any(input => !nodes.Contains(input));
			});

			if (firstRunnableNode != null) {
				firstRunnableNode = substitutes[firstRunnableNode] as RunnableNode;
			}

			if (firstRunnableNode != null && actionVerb) {
				var rootNode = actionVerb.GetRootNode();
				if (rootNode == null) {
					rootNode = actionVerb.AddNode<RootNode>();
					rootNode.name = "Root";
				}
				rootNode.position = firstRunnableNode.position + new Vector2(-200, 0);
				var outputPort = rootNode.GetOutputPort(nameof(RootNode.runNode));
				var inputPort = firstRunnableNode.GetThisNodePort();
				inputPort.Connect(outputPort);
			}

			var inputPortNameTable = new Dictionary<NodePort, string>();

			// find any connections from selected nodes to non-selected nodes
			foreach (var selectedNode in nodes) {
				foreach (var port in selectedNode.Ports) {
					for (int c = 0; c < port.ConnectionCount; c++) {

						NodePort inputPort = port.direction == NodePort.IO.Input ? port : port.GetConnection(c);
						NodePort outputPort = port.direction == NodePort.IO.Output ? port : port.GetConnection(c);

						if (inputPort.node == selectedNode && !nodes.Contains(outputPort.node)) {
							if (substitutes.TryGetValue(selectedNode, out var newNode)) {

								if (inputPort.ValueType == typeof(RunnableNode)) {

								}
								else {
									// create a verb input port
									var inputName = inputPort.fieldName;
									if (outputPort.node is GetVariableNode variableNode) {
										var variable = variableNode.GetVariable();
										if (variable != null) {
											inputName = variable.Name;
										}
									}
									inputPortNameTable[inputPort] = inputName;
									var graphInput = newGraph.GetOrAddInput(inputPort.ValueType, inputName);
									var getVariableNode = newGraph.CreateNode<GetVariableNode>(newNode.position + new Vector2(-300, 0));
									getVariableNode.SetVariable(SerializableVariableReference.ScopeType.Verb, graphInput);
									getVariableNode.UpdatePorts();
									newNode.UpdatePorts();
									var newNodeInputPort = newNode.GetInputPort(inputPort.fieldName);
									var getGraphVariableOutputPort = getVariableNode.GetOutputPort(SerializableVariableReference.PORT_NAME);
									if (!newNodeInputPort.IsConnectedTo(getGraphVariableOutputPort)) {
										newNodeInputPort.Connect(getGraphVariableOutputPort);
									}
								}
							}
						}
						else
						if (outputPort.node == selectedNode && !nodes.Contains(inputPort.node)) {
							if (substitutes.TryGetValue(selectedNode, out var newNode)) {
								var newNodeOutputPort = newNode.GetOutputPort(outputPort.fieldName);

								if (outputPort.ValueType == typeof(RunnableNode)) {

								}
								else {
									// create a graph output port
									var graphOutput = newGraph.GetOrAddOutput(outputPort.ValueType, outputPort.fieldName);

									if (actionVerb) {
										var setVerbVariableNode = actionVerb.CreateNode<SetVariableNode>((lastRunnableNode ?? newNode).position + new Vector2(300, 0));
										setVerbVariableNode.SetVariable(SerializableVariableReference.ScopeType.Verb, graphOutput);
										setVerbVariableNode.UpdatePorts();
										newNode.UpdatePorts();
										var getGraphVariableOutputPort = setVerbVariableNode.GetInputPort(outputPort.fieldName);

										if (!newNodeOutputPort.IsConnectedTo(getGraphVariableOutputPort)) {
											newNodeOutputPort.Connect(getGraphVariableOutputPort);
										}

										lastRunnableNode?.ConnectDownstream(setVerbVariableNode);
										lastRunnableNode = setVerbVariableNode;
									}
									else {
										var outputNode = newGraph.GetOutputNode();
										outputNode.RebuildInputPorts();
										var nodeInputPort = outputNode.GetInputPort(outputPort.fieldName);

										outputNode.position = newNode.position + new Vector2(300, 0);

										if (!newNodeOutputPort.IsConnectedTo(nodeInputPort)) {
											newNodeOutputPort.Connect(nodeInputPort);
										}
									}
								}
							}
						}
					}
				}
			}

			Node runGraphNode;
			var averagePoint = nodes.Select(node => node.position).Average();

			if (actionVerb) {
				var runActionVerbNode = graph.CreateNode<RunActionVerbNode>(averagePoint);
				runActionVerbNode.actionVerb = actionVerb;

				runActionVerbNode.UpdatePorts();

				runGraphNode = runActionVerbNode;
			}
			else {
				var runValueVerbNode = graph.CreateNode<RunValueVerbNode>(averagePoint);
				runValueVerbNode.valueVerb = newGraph as ValueVerb;

				runValueVerbNode.UpdatePorts();

				runGraphNode = runValueVerbNode;
			}


			foreach (var selectedNode in nodes) {
				foreach (var port in selectedNode.Ports) {
					for (int c = 0; c < port.ConnectionCount; c++) {

						NodePort inputPort = port.direction == NodePort.IO.Input ? port : port.GetConnection(c);
						NodePort outputPort = port.direction == NodePort.IO.Output ? port : port.GetConnection(c);

						if (inputPort.node == selectedNode && !nodes.Contains(outputPort.node)) {
							// one of the selected nodes takes an input from a node outside of the selected nodes
							if (inputPort.ValueType == typeof(ActionVerb)) {
								var runNodePort = runGraphNode.GetInputPort(RunnableNode.ThisNodeField);
								outputPort.Connect(runNodePort);
							}
							else {

								var inputName = inputPortNameTable.ContainsKey(inputPort) ? inputPortNameTable[inputPort] : inputPort.fieldName;
								var runNodePort = runGraphNode.GetInputPort(inputName);
								outputPort.Connect(runNodePort);
							}
						}
						else
						if (outputPort.node == selectedNode && !nodes.Contains(inputPort.node)) {
							// one of the selected nodes takes an output from a node outside of the selected nodes
							if (outputPort.ValueType == typeof(ActionVerb)) {
								var runNodePort = runGraphNode.GetInputPort(nameof(ChainedRunnableNode.thenRunNode));
								outputPort.Connect(runNodePort);
							}
							else {
								var runNodePort = runGraphNode.GetOutputPort(outputPort.fieldName);
								inputPort.Connect(runNodePort);
							}
						}
					}
				}
			}

			if (newGraph is ValueVerb) {
				if (newGraph.TryGetOutputNode(out var outputNodeB)) {
					var farthestRightNode = newGraph.nodes.Excluding(outputNodeB).OrderByDescending(x => x.position.x).FirstOrDefault();
					if (farthestRightNode != null) {
						outputNodeB.position = farthestRightNode.position + Vector2.right * 300;
					}
				}
			}


			foreach (var node in nodes) {
				graph.RemoveNode(node);
				Undo.DestroyObjectImmediate(node);
			}
		}

		public static Node CreateNode(this NodeGraph graph, Type type, Vector2 position) {
			XNode.Node node = graph.AddNode(type);
			Undo.RegisterCreatedObjectUndo(node, "Create Node");
			node.position = position;
			if (node.name == null || node.name.Trim() == "") {
				node.name = NodeEditorUtilities.NodeDefaultName(type);
			}
			if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(graph))) {
				AssetDatabase.AddObjectToAsset(node, graph);
			}
			return node;
		}

		public static T CreateNode<T>(this NodeGraph graph, Vector2 position) where T : Node {
			XNode.Node node = graph.AddNode<T>();
			Undo.RegisterCreatedObjectUndo(node, "Create Node");
			node.position = position;
			if (node.name == null || node.name.Trim() == "") {
				node.name = NodeEditorUtilities.NodeDefaultName(typeof(T));
			}
			if (!string.IsNullOrEmpty(AssetDatabase.GetAssetPath(graph))) {
				AssetDatabase.AddObjectToAsset(node, graph);
			}
			return node as T;
		}

	}
}