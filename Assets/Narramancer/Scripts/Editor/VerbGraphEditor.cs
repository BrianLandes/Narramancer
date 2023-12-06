
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using XNode;
using XNodeEditor;

namespace Narramancer {

	[CustomNodeGraphEditor(typeof(VerbGraph), "VerbGraphEditor.Settings")]
	public class VerbGraphEditor : NodeGraphEditor {

		// https://github.com/Siccity/xNode/wiki/Graph-Editors

		private static List<VerbGraph> lastOpenedStack => NarramancerSingleton.Instance.RecentlyOpenedGraphs;
		private static bool didBack = false;

		public override void OnOpen() {

			if (didBack) {
				didBack = false;
				var thisGraphInStack = lastOpenedStack.LastOrDefault(graph => graph == window.graph);
				if (thisGraphInStack != null) {
					var index = lastOpenedStack.IndexOf(thisGraphInStack);

					if (index == lastOpenedStack.Count() - 2) {
						lastOpenedStack.RemoveAt(index + 1);
					}
				}
			}
			else
			if (lastOpenedStack.Any()) {
				var peek = lastOpenedStack.Last();
				if (peek != window.graph) {
					lastOpenedStack.Add(window.graph as VerbGraph);
				}
			}
			else if (window.graph != null) {
				lastOpenedStack.Add(window.graph as VerbGraph);
			}

			window.titleContent = new GUIContent(target.name);
		}


		public override string GetNodeMenuName(Type type) {
			if (target is ValueVerb) {
				if (typeof(RunnableNode).IsAssignableFrom(type)) {
					return null;
				}
				if (typeof(RootNode).IsAssignableFrom(type)) {
					return null;
				}
			}
			else
			if (target is ActionVerb) {
				if (typeof(OutputNode).IsAssignableFrom(type)) {
					return null;
				}
			}

			if (typeof(Node).IsAssignableFrom(type)) {
				return base.GetNodeMenuName(type);
			}
			else {

				return null;
			}
		}


		public override NodeEditorPreferences.Settings GetDefaultPreferences() {

			ColorUtility.TryParseHtmlString("#030C11", out var gridBgColor);
			ColorUtility.TryParseHtmlString("#001829", out var gridLineColor);

			ColorUtility.TryParseHtmlString("#63768D", out var stringTypeColor);
			ColorUtility.TryParseHtmlString("#F7EE7F", out var intTypeColor);
			ColorUtility.TryParseHtmlString("#A54657", out var runnableNodeTypeColor);


			return new NodeEditorPreferences.Settings() {
				gridBgColor = gridBgColor,
				gridLineColor = gridLineColor,
				noodleStroke = NoodleStroke.Full,
				typeColors = new Dictionary<string, Color>() {
			{ typeof(string).PrettyName(), stringTypeColor },
			{ typeof(int).PrettyName(), intTypeColor },
			{ typeof(RunnableNode).PrettyName(), runnableNodeTypeColor }
		}
			};
		}

		public override void OnGUI() {

			GUILayout.BeginArea(new Rect(0, 0, Screen.width, 30));

			GUILayout.BeginHorizontal();

			var otherGraph = lastOpenedStack.LastOrDefault(graph => graph != window.graph);
			EditorGUI.BeginDisabledGroup(otherGraph == null);
			if (GUILayout.Button("Back")) {
				didBack = true;
				AssetDatabase.OpenAsset(otherGraph);
			}
			EditorGUI.EndDisabledGroup();

			if (GUILayout.Button($"Verb: {window.graph.name}")) {
				EditorGUIUtility.PingObject(window.graph);
			}

			{
				var assetPath = AssetDatabase.GetAssetPath(window.graph);
				var assetName = Path.GetFileNameWithoutExtension(assetPath);
				if (GUILayout.Button($"Asset: {assetName}")) {
					var mainAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
					EditorGUIUtility.PingObject(mainAsset);
				}
			}

			GUILayout.EndHorizontal();

			GUILayout.EndArea();

			#region Selected Drop Down Box
			GUILayout.BeginArea(new Rect(0, 40, 200, Screen.height));
			GUILayout.BeginVertical();

			IEnumerable< NodeRunnerUnityObjectPairing > GetPossiblePairings() {
				var narramancerScenes = GameObject.FindObjectsOfType<NarramancerScene>();
				foreach( var scene in narramancerScenes) {
					foreach (var nodeRunner in scene.NodeRunners) {
						yield return new NodeRunnerUnityObjectPairing() {
							name = $"{scene.gameObject.name} - {nodeRunner.name}" ,
							nodeRunner = nodeRunner,
							unityObject = scene.gameObject
						};
					}
					yield return new NodeRunnerUnityObjectPairing() {
						name = scene.gameObject.name,
						nodeRunner = null, // applying null here will allow the GameObject it self to show up even if nothing is running
						unityObject = scene.gameObject
					};

				}

				// TODO: include RunActionVerbMonoBehaviours

				// TODO: include NarramancerSingleton
			}

			if (window.selectedNodeRunnerPairing == null || window.selectedNodeRunnerPairing.unityObject == null) {
				var possibleHolders = GetPossiblePairings();
				window.selectedNodeRunnerPairing = possibleHolders.FirstOrDefault();
			}

			var selectionText = window.selectedNodeRunnerPairing != null ? window.selectedNodeRunnerPairing.name : "(None)";

			if (EditorGUILayout.DropdownButton(new GUIContent(selectionText, selectionText), FocusType.Passive)) {
				GenericMenu context = new GenericMenu();

				var possibleHolders = GetPossiblePairings();
				if (possibleHolders.Any()) {
					foreach (var holder in possibleHolders) {
						bool IsHolderSelected() {
							if ( window.selectedNodeRunnerPairing == null) {
								return false;
							}
							return holder.unityObject == window.selectedNodeRunnerPairing.unityObject && holder.nodeRunner == window.selectedNodeRunnerPairing.nodeRunner;
						}

						context.AddItem(new GUIContent(holder.name, holder.name), IsHolderSelected(), () => {
							EditorGUIUtility.PingObject(holder.unityObject);
							Selection.activeObject = holder.unityObject;
							window.selectedNodeRunnerPairing = holder;
						});
					}
					
				} else {
					context.AddDisabledItem(new GUIContent("(No valid values)"));
				}

				

				Matrix4x4 originalMatrix = GUI.matrix;
				GUI.matrix = Matrix4x4.identity;
				context.ShowAsContext();
				GUI.matrix = originalMatrix;
			}

			GUILayout.EndVertical();
			GUILayout.EndArea();
			#endregion

		}

		public override void OnDropObjects(UnityEngine.Object[] objects) {

			var nodeEditorWindow = EditorWindow.GetWindow<NodeEditorWindow>();
			if (nodeEditorWindow.IsHoveringNode) {
				return;
			}

			var position = nodeEditorWindow.WindowToGridPosition(Event.current.mousePosition);

			foreach (var draggedObject in objects.WithoutNulls()) {
				switch (draggedObject) {
					case ActionVerb actionVerb: {
							if (target is ActionVerb) {
								var newNode = CreateNode(typeof(RunActionVerbNode), position) as RunActionVerbNode;
								newNode.actionVerb = actionVerb;
							}
							break;
						}

					case ValueVerb valueVerb: {
							var newNode = CreateNode(typeof(RunValueVerbNode), position) as RunValueVerbNode;
							newNode.valueVerb = valueVerb;
							break;
						}
					case NounScriptableObject nounScriptableObject: {
							var newNode = CreateNode(typeof(GetInstanceNode), position) as GetInstanceNode;
							newNode.Noun = nounScriptableObject;
							break;
						}
					case UnityEngine.Object unityObject: {
							var newNode = CreateNode(typeof(UnityObjectNode), position) as UnityObjectNode;
							newNode.SetObject(unityObject);
							break;
						}
				}

				position.x += 200f;
			}

		}

		public override void AddContextMenuItemsForNewNodes(GenericMenu menu) {

			Vector2 pos = NodeEditorWindow.current.WindowToGridPosition(Event.current.mousePosition);
			var mousePosition = Event.current.mousePosition;
			var screenPosition = GUIUtility.GUIToScreenPoint(mousePosition);

			menu.AddItem(new GUIContent("Search..."), false, () => {
				NodeSearchModalWindow window = ScriptableObject.CreateInstance(typeof(NodeSearchModalWindow)) as NodeSearchModalWindow;
				var allowRunnableNodes = typeof(ActionVerb).IsAssignableFrom(target.GetType());
				window.ShowForValues(screenPosition, allowRunnableNodes, type => {
					XNode.Node node = CreateNode(type, pos);
					NodeEditorWindow.current.AutoConnect(node);
				});
			});

			base.AddContextMenuItemsForNewNodes(menu);

			menu.AddSeparator("");


			menu.AddItem(new GUIContent("Create Child Action Verb"), false, () => {
				var graph = NodeEditorWindow.current.graph;
				var name = $"{graph.name} (Verb)";
				var actionVerb = PseudoEditorUtilities.CreateAndAddChild<ActionVerb>(graph, name);
				var runActionVerbNode = CreateNode(typeof(RunActionVerbNode), pos) as RunActionVerbNode;
				runActionVerbNode.actionVerb = actionVerb;
				runActionVerbNode.name = $"Run {name}";
			});

			var selectedNodes = Selection.objects.Where(@object => @object is Node).Cast<Node>().ToArray();

			if (selectedNodes.Any()) {
				menu.AddItem(new GUIContent("Collapse"), false, () => {
					target.Collapse(selectedNodes);
				});
			}

		}

		float spacing = 30;

		public override void AddContextMenuItemsForSelectedNodes(GenericMenu menu) {
			var selectedNodes = Selection.objects
				.Where(@object => @object is Node)
				.Cast<Node>()
				.ToArray();

			if (selectedNodes.Any()) {

				var selectedNodesNoRoot = selectedNodes
					.Where(@object => !(@object is RootNode))
					.Where(@object => !(@object is GetVariableNode getVariableNode && getVariableNode.Scope == SerializableVariableReference.ScopeType.Verb))
					.ToArray();
				if (selectedNodesNoRoot.Count() >= 1) {
					menu.AddItem(new GUIContent("Collapse"), false, () => {
						target.Collapse(selectedNodesNoRoot);
					});
				}


				menu.AddItem(new GUIContent("Align Horizontally"), false, () => {

					var orderedNodes = selectedNodes.OrderBy(node => node.position.x).ToArray();
					var position = orderedNodes.First().position;
					foreach (var node in orderedNodes) {
						node.position = position;
						position.x += NodeEditorWindow.current.nodeSizes[node].x + spacing;
					}
				});

				var selectedRunnableNodes = selectedNodes.Where(node => node is ChainedRunnableNode).Cast<ChainedRunnableNode>().ToArray();

				if (selectedRunnableNodes.Count() == 2) {
					menu.AddItem(new GUIContent("Swap"), false, () => {
						var orderedNodes = selectedRunnableNodes.OrderBy(node => node.position.x).ToArray();
						var leftNode = orderedNodes[0];
						var rightNode = orderedNodes[1];

						rightNode.position = leftNode.position;
						leftNode.position = rightNode.position + new Vector2(NodeEditorWindow.current.nodeSizes[rightNode].x + spacing, 0);

						// if the left node had a connection coming in, move that to the node on the right
						var leftInputPort = leftNode.GetThisNodePort();
						if (leftInputPort.IsConnected) {
							var rightInputPort = rightNode.GetThisNodePort();
							leftInputPort.MoveConnections(rightInputPort);
						}

						var leftOutputPort = leftNode.GetNextNodePort();
						var rightOutputPort = rightNode.GetNextNodePort();

						// if the left node had a connection going out into the right node, mirror the connection and remove it
						var connectedToEachOther = leftOutputPort.IsConnected && leftOutputPort.Connection.node == rightNode;


						// if the right node had a connection going out, move that to the node on the left
						var rightOutputConnectedTo = rightOutputPort.IsConnected ? rightOutputPort.Connection : null;

						if (connectedToEachOther) {
							leftInputPort.Connect(rightOutputPort);
							leftOutputPort.Disconnect(leftOutputPort.Connection);
						}

						if (rightOutputConnectedTo != null) {
							leftOutputPort.Connect(rightOutputConnectedTo);
						}
					});
				}
			}
		}

	}
}