
using System;
using System.Collections.Generic;
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
		public static NodeRunner selectedNodeRunner;
		public static Texture2D triangle { get { return _triangle != null ? _triangle : _triangle = Resources.Load<Texture2D>("xnode_triangle"); } }
		private static Texture2D _triangle;

		public GUIStyle runnablePortStyle;

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

		public override GUIStyle GetPortStyle(NodePort port) {
			// A dynamic port has no ValueType while it is mid-rebuild, and the editor still repaints during
			// that window, so both of these have to be tolerated rather than assumed.
			if (port == null || port.ValueType == null) {
				return base.GetPortStyle(port);
			}
			if (port.ValueType.IsAssignableFrom(typeof(RunnableNode))) {
				if (runnablePortStyle == null) {

					runnablePortStyle = new GUIStyle(NodeEditorResources.styles.inputPort);
					runnablePortStyle.alignment = TextAnchor.UpperLeft;
					runnablePortStyle.padding.left = 0;
					runnablePortStyle.active.background = triangle;
					runnablePortStyle.normal.background = triangle;
				}
				return runnablePortStyle;
			}

			return base.GetPortStyle(port);
		}

		public override void OnGUI() {

			if (runnablePortStyle == null) {

				runnablePortStyle = new GUIStyle(NodeEditorResources.styles.inputPort);
				runnablePortStyle.alignment = TextAnchor.UpperLeft;
				runnablePortStyle.padding.left = 0;
				runnablePortStyle.active.background = triangle;
				runnablePortStyle.normal.background = triangle;
			}

			UpdateSelectedNodeRunner();

			var rowRect = new Rect(0, 0, window.position.width, ToolbarRowHeight);
			GUILayout.BeginArea(rowRect, EditorStyles.toolbar);
			GUILayout.BeginHorizontal();
			DrawNavigation();
			GUILayout.FlexibleSpace();
			DrawTools();
			GUILayout.EndHorizontal();
			GUILayout.EndArea();

			if (Application.isPlaying) {
				rowRect.y += ToolbarRowHeight;
				GUILayout.BeginArea(rowRect, EditorStyles.toolbar);
				GUILayout.BeginHorizontal();
				DrawRunnerSelection();
				GUILayout.FlexibleSpace();
				GUILayout.EndHorizontal();
				GUILayout.EndArea();
			}
		}

		#region Toolbar

		private const float MaxNameWidth = 220;
		private const int MaxRecentGraphs = 15;
		/// <summary>Matches <see cref="AbstractSearchModalWindow{T}"/>'s default width, to keep a popup opened from the right edge on screen.</summary>
		private const float SearchPopupWidth = 500;

		private static float ToolbarRowHeight => EditorStyles.toolbar.fixedHeight > 0 ? EditorStyles.toolbar.fixedHeight : 21;

		/// <summary>The runner row only shows while playing, when there are runners to watch.</summary>
		private int ToolbarRowCount => Application.isPlaying ? 2 : 1;

		public override bool IsOverlayAt(Vector2 windowPosition) {
			return windowPosition.y >= 0 && windowPosition.y < ToolbarRowHeight * ToolbarRowCount;
		}

		private void DrawNavigation() {

			var previousGraph = lastOpenedStack.LastOrDefault(graph => graph != null && graph != window.graph);
			using (new EditorGUI.DisabledScope(previousGraph == null)) {
				var tooltip = previousGraph != null ? $"Back to {previousGraph.name}" : "No previous verb";
				if (GUILayout.Button(new GUIContent("Back", tooltip), EditorStyles.toolbarButton)) {
					OpenGraph(previousGraph, isBack: true);
				}
			}

			if (GUILayout.Button(new GUIContent("Recent", "Verbs opened recently"), EditorStyles.toolbarDropDown)) {
				var recentGraphs = lastOpenedStack.AsEnumerable()
					.Reverse()
					.Where(graph => graph != null && graph != window.graph)
					.Distinct()
					.Take(MaxRecentGraphs);
				var menu = new GenericMenu();
				AddGraphItems(menu, string.Empty, recentGraphs, includeAssetName: true);
				ShowMenu(menu);
			}

			GUILayout.Space(6);

			// Breadcrumb: the asset the verb lives in, then the verb itself
			var assetPath = AssetDatabase.GetAssetPath(window.graph);
			var mainAsset = AssetDatabase.LoadMainAssetAtPath(assetPath);
			if (mainAsset != null && mainAsset != window.graph) {
				var assetContent = new GUIContent(mainAsset.name, AssetPreview.GetMiniThumbnail(mainAsset), $"{assetPath}\nSelect the asset this verb lives in");
				if (GUILayout.Button(assetContent, EditorStyles.toolbarButton, GUILayout.MaxWidth(MaxNameWidth))) {
					Selection.activeObject = mainAsset;
					EditorGUIUtility.PingObject(mainAsset);
				}
				GUILayout.Label("›", GUILayout.ExpandWidth(false));
			}

			var graphTooltip = $"{window.graph.GetType().Name.Nicify()}\nSelect or ping this verb, or switch to another verb in the same asset";
			var graphContent = new GUIContent(window.graph.name, AssetPreview.GetMiniThumbnail(window.graph), graphTooltip);
			if (GUILayout.Button(graphContent, EditorStyles.toolbarDropDown, GUILayout.MaxWidth(MaxNameWidth))) {
				var graph = window.graph;
				var menu = new GenericMenu();
				menu.AddItem(new GUIContent("Select Verb"), false, () => Selection.activeObject = graph);
				menu.AddItem(new GUIContent("Ping in Project"), false, () => EditorGUIUtility.PingObject(graph));
				menu.AddSeparator(string.Empty);
				menu.AddDisabledItem(new GUIContent("Verbs in this asset"));
				var siblingGraphs = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<VerbGraph>().OrderBy(sibling => sibling.name);
				AddGraphItems(menu, string.Empty, siblingGraphs, includeAssetName: false);
				ShowMenu(menu);
			}

			GUILayout.Space(6);

			if (GUILayout.Button(new GUIContent("Linked", "Verbs this verb runs, and verbs that run it"), EditorStyles.toolbarDropDown)) {
				var graph = target as VerbGraph;
				var usedBy = GetAllVerbGraphs().Where(other => other != graph && GetReferencedGraphs(other).Contains(graph));
				var menu = new GenericMenu();
				AddGraphItems(menu, "Uses/", GetReferencedGraphs(graph), includeAssetName: true);
				AddGraphItems(menu, "Used By/", usedBy, includeAssetName: true);
				ShowMenu(menu);
			}

			if (GUILayout.Button(new GUIContent("Find Verb", "Search every verb in the project"), EditorStyles.toolbarButton)) {
				var buttonRect = GUILayoutUtility.GetLastRect();
				var popup = ScriptableObject.CreateInstance<VerbSearchModalWindow>();
				popup.ShowForValues(ToScreenPosition(new Vector2(buttonRect.x, buttonRect.yMax)), GetAllVerbGraphs(), graph => OpenGraph(graph));
			}
		}

		private void DrawTools() {

			if (GUILayout.Button(new GUIContent("Add Node", "Search for a node to add at the center of the view (or double-click the graph)"), EditorStyles.toolbarButton)) {
				var buttonRect = GUILayoutUtility.GetLastRect();
				var popupPosition = new Vector2(Mathf.Max(0, buttonRect.xMax - SearchPopupWidth), buttonRect.yMax);
				EditorDrawerUtilities.ShowSearchNodesPopup(target, GetViewCenter(), ToScreenPosition(popupPosition));
			}

			if (GUILayout.Button(new GUIContent("Frame", "Center the view on the selected nodes, or reset it when nothing is selected (F)"), EditorStyles.toolbarButton)) {
				window.Home();
			}

			var collapsibleNodes = GetCollapsibleSelection();
			using (new EditorGUI.DisabledScope(collapsibleNodes.Length == 0)) {
				if (GUILayout.Button(new GUIContent("Collapse", "Move the selected nodes into a new child verb, replaced here by a node that runs it"), EditorStyles.toolbarButton)) {
					target.Collapse(collapsibleNodes);
				}
			}

			if (target is ActionVerb) {
				if (GUILayout.Button(new GUIContent("New Child Verb", "Create an Action Verb inside this asset, and a node here that runs it"), EditorStyles.toolbarButton)) {
					CreateChildActionVerb(GetViewCenter());
				}
			}

			GUILayout.Space(6);

			if (GUILayout.Button(new GUIContent("More"), EditorStyles.toolbarDropDown)) {
				var menu = new GenericMenu();
				menu.AddItem(new GUIContent("Open Narramancer"), false, NarramancerSingletonEditor.OpenWindow);
				menu.AddItem(new GUIContent("Graph Editor Preferences"), false, NodeEditorReflection.OpenPreferences);
				ShowMenu(menu);
			}
		}

		private void DrawRunnerSelection() {

			GUILayout.Label(new GUIContent("Watching", "The runner whose progress is highlighted in the graph"), GUILayout.ExpandWidth(false));

			var selectionText = window.selectedNodeRunnerUnityObject != null ? window.selectedNodeRunnerUnityObject.name : "(None)";

			if (GUILayout.Button(new GUIContent(selectionText, selectionText), EditorStyles.toolbarDropDown, GUILayout.MaxWidth(MaxNameWidth))) {
				GenericMenu context = new GenericMenu();

				var possibleObjects = GetPossibleNodeRunnerObjects();
				if (possibleObjects.Any()) {
					var usedNames = new HashSet<string>();
					foreach (var unityObject in possibleObjects) {

						var name = unityObject.name;
						while (usedNames.Contains(name)) {
							name += "*";
						}
						usedNames.Add(name);
						context.AddItem(new GUIContent(name, name), unityObject == window.selectedNodeRunnerUnityObject, () => {
							EditorGUIUtility.PingObject(unityObject);
							Selection.activeObject = unityObject;
							window.selectedNodeRunnerUnityObject = unityObject;
							selectedNodeRunner = null;
						});
					}

				}
				else {
					context.AddDisabledItem(new GUIContent("(No valid values)"));
				}

				ShowMenu(context);
			}

			selectionText = selectedNodeRunner != null ? selectedNodeRunner.name : "(None)";

			if (GUILayout.Button(new GUIContent(selectionText, selectionText), EditorStyles.toolbarDropDown, GUILayout.MaxWidth(MaxNameWidth))) {
				GenericMenu context = new GenericMenu();

				var possibleNodeRunners = GetPossibleNodeRunners();
				if (possibleNodeRunners.Any()) {
					foreach (var nodeRunner in possibleNodeRunners) {

						context.AddItem(new GUIContent(nodeRunner.name, nodeRunner.name), selectedNodeRunner == nodeRunner, () => {
							selectedNodeRunner = nodeRunner;
							window.selectedContext = selectedNodeRunner?.Blackboard;
						});
					}

				}
				else {
					context.AddDisabledItem(new GUIContent("(No valid values)"));
				}

				ShowMenu(context);
			}
		}

		private void UpdateSelectedNodeRunner() {

			if (window.selectedNodeRunnerUnityObject == null) {
				window.selectedNodeRunnerUnityObject = GetPossibleNodeRunnerObjects().FirstOrDefault();
			}

			if (!Application.isPlaying) {
				selectedNodeRunner = null;
				window.selectedContext = null;
				return;
			}

			// A load replaces the story's runners with freshly read ones, and a scene change replaces the object
			// that owned them. Keeping the old reference showed the graph a runner nothing ticks any more, so the
			// highlight froze on the pre-load node at exactly the moment it should show the resume.
			if (selectedNodeRunner != null && !GetPossibleNodeRunners().Contains(selectedNodeRunner)) {
				selectedNodeRunner = null;
			}

			if (selectedNodeRunner == null) {
				selectedNodeRunner = GetPossibleNodeRunners().FirstOrDefault();
				window.selectedContext = selectedNodeRunner?.Blackboard;
			}
		}

		private static IEnumerable<UnityEngine.Object> GetPossibleNodeRunnerObjects() {
			var narramancerScenes = GameObjectExtensions.FindObjectsOfType<NarramancerScene>();
			foreach (var scene in narramancerScenes) {
				yield return scene;
			}

			var runActionVerbMonoBehaviours = GameObjectExtensions.FindObjectsOfType<RunActionVerbMonoBehaviour>();
			foreach (var runActionVerb in runActionVerbMonoBehaviours) {
				yield return runActionVerb;
			}

			// TODO: include NarramancerSingleton
		}

		private IEnumerable<NodeRunner> GetPossibleNodeRunners() {
			if (window.selectedNodeRunnerUnityObject != null) {
				if (window.selectedNodeRunnerUnityObject is NarramancerScene scene) {
					foreach (var nodeRunner in scene.NodeRunners) {
						yield return nodeRunner;
					}
				}
				else
				if (window.selectedNodeRunnerUnityObject is RunActionVerbMonoBehaviour runActionVerb) {
					yield return runActionVerb.Runner;
				}
			}

			// TODO: include NarramancerSingleton
		}

		/// <summary>Opens the graph once this GUI pass is over, so the rest of the toolbar doesn't draw half for one graph and half for the next.</summary>
		private static void OpenGraph(VerbGraph graph, bool isBack = false) {
			EditorApplication.delayCall += () => {
				didBack = isBack;
				NodeEditorWindow.Open(graph);
			};
		}

		private void AddGraphItems(GenericMenu menu, string submenu, IEnumerable<VerbGraph> graphs, bool includeAssetName) {
			// GenericMenu merges items whose paths match, so repeated names are made distinct
			var usedLabels = new HashSet<string>();
			foreach (var graph in graphs) {
				var label = submenu + GetMenuLabel(graph, includeAssetName);
				while (usedLabels.Contains(label)) {
					label += "*";
				}
				usedLabels.Add(label);
				menu.AddItem(new GUIContent(label), graph == window.graph, () => OpenGraph(graph));
			}
			if (usedLabels.Count == 0) {
				menu.AddDisabledItem(new GUIContent(submenu + "(None)"));
			}
		}

		private static string GetMenuLabel(VerbGraph graph, bool includeAssetName) {
			// A '/' in a menu label starts a submenu
			var label = graph.name.Replace('/', '-');
			if (includeAssetName) {
				var mainAsset = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(graph));
				if (mainAsset != null && mainAsset != graph) {
					label += $"  ({mainAsset.name.Replace('/', '-')})";
				}
			}
			return label;
		}

		private static IEnumerable<VerbGraph> GetAllVerbGraphs() {
			return PseudoEditorUtilities.GetAllInstances<VerbGraph>().Distinct().OrderBy(graph => graph.name);
		}

		/// <summary>The verbs any node in the graph refers to, such as the verb a Run Action Verb node runs.</summary>
		private static IEnumerable<VerbGraph> GetReferencedGraphs(VerbGraph graph) {
			var referencedGraphs = new HashSet<VerbGraph>();
			foreach (var node in graph.nodes) {
				if (node == null) {
					continue;
				}
				using (var serializedNode = new SerializedObject(node)) {
					var property = serializedNode.GetIterator();
					while (property.Next(true)) {
						if (property.propertyType == SerializedPropertyType.ObjectReference
							&& property.objectReferenceValue is VerbGraph referencedGraph
							&& referencedGraph != graph) {
							referencedGraphs.Add(referencedGraph);
						}
					}
				}
			}
			return referencedGraphs.OrderBy(referencedGraph => referencedGraph.name);
		}

		/// <summary>The toolbar sits in the window's top-left corner, so its positions are window positions.</summary>
		private Vector2 ToScreenPosition(Vector2 windowPosition) {
			return window.position.position + windowPosition;
		}

		private Vector2 GetViewCenter() {
			return window.WindowToGridPosition(window.position.size * 0.5f);
		}

		/// <summary>The graph's zoom leaves a matrix behind that would misplace the menu, so it is shown without one.</summary>
		private static void ShowMenu(GenericMenu menu) {
			Matrix4x4 originalMatrix = GUI.matrix;
			GUI.matrix = Matrix4x4.identity;
			menu.ShowAsContext();
			GUI.matrix = originalMatrix;
		}

		#endregion

		#region Shared Operations

		/// <summary>The selected nodes that can be collapsed into a child verb: the root and the verb's own inputs stay behind.</summary>
		private Node[] GetCollapsibleSelection() {
			return Selection.objects
				.OfType<Node>()
				.Where(node => node.graph == target)
				.Where(node => !(node is RootNode))
				.Where(node => !(node is GetVariableNode getVariableNode && getVariableNode.Scope == SerializableVariableReference.ScopeType.Verb))
				.ToArray();
		}

		private void CreateChildActionVerb(Vector2 position) {
			var name = $"{target.name} (Verb)";
			var actionVerb = PseudoEditorUtilities.CreateAndAddChild<ActionVerb>(target, name);
			var runActionVerbNode = CreateNode(typeof(RunActionVerbNode), position) as RunActionVerbNode;
			runActionVerbNode.actionVerb = actionVerb;
			runActionVerbNode.name = $"Run {name}";
		}

		#endregion

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
					case StatScriptableObject statScriptableObject: {
							var newNode = CreateNode(typeof(GetStatNode), position) as GetStatNode;
							newNode.stat = statScriptableObject;
							break;
						}
					case NounScriptableObject nounScriptableObject: {
							var newNode = CreateNode(typeof(GetInstanceNode), position) as GetInstanceNode;
							newNode.Noun = nounScriptableObject;
							break;
						}
					case GameObject gameObject: {
							var menu = new GenericMenu();

							menu.AddItem(new GUIContent("GameObject"), false, () => {
								var newNode = CreateNode(typeof(UnityObjectNode), position) as UnityObjectNode;
								newNode.SetObject(gameObject);
							});

							foreach (var component in gameObject.GetComponents<Component>()) {

								menu.AddItem(new GUIContent(component.GetType().Name), false, () => {
									var newNode = CreateNode(typeof(UnityObjectNode), position) as UnityObjectNode;
									newNode.SetObject(component);
								});
							}


							ShowMenu(menu);
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

		public override void AddContextMenuItemsForNewNodes(GenericMenu menu, Type compatibleType = null, NodePort.IO direction = NodePort.IO.Input) {

			Vector2 pos = NodeEditorWindow.current.WindowToGridPosition(Event.current.mousePosition);
			var mousePosition = Event.current.mousePosition;
			var screenPosition = GUIUtility.GUIToScreenPoint(mousePosition);

			menu.AddItem(new GUIContent("Search..."), false, () => {
				EditorDrawerUtilities.ShowSearchNodesPopup(target, mousePosition);
			});

			base.AddContextMenuItemsForNewNodes(menu);

			menu.AddSeparator("");


			// A Run Action Verb node is runnable, which a Value Verb can't hold
			if (target is ActionVerb) {
				menu.AddItem(new GUIContent("Create Child Action Verb"), false, () => CreateChildActionVerb(pos));
			}

			var collapsibleNodes = GetCollapsibleSelection();

			if (collapsibleNodes.Any()) {
				menu.AddItem(new GUIContent("Collapse"), false, () => {
					target.Collapse(collapsibleNodes);
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

				var collapsibleNodes = GetCollapsibleSelection();
				if (collapsibleNodes.Any()) {
					menu.AddItem(new GUIContent("Collapse"), false, () => {
						target.Collapse(collapsibleNodes);
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



		public override void DoubleClick() {
			var mousePosition = Event.current.mousePosition;
			EditorDrawerUtilities.ShowSearchNodesPopup(target, mousePosition);
		}

	}
}