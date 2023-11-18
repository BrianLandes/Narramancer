//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using UnityEditor;
//using UnityEditor.Callbacks;
//using UnityEngine;

//namespace Narramancer {
//	public class NarramancerEditorWindow : EditorWindow {

//		public StoryScriptableObject story;
//		private int tab;
//		private int nounTypeFilter;
//		private int adjectiveTypeFilter;

//		//private float assetSelectorWidth = 300f;
//		//private bool resizeDraggingAsset = false;

//		//private float inspectorWidth = 300f;
//		//private bool draggingInspector = false;

//		//private float resizeDragStartPosition = 0f;

//		GUIContent elementContent;

//		[MenuItem("Window/Narramancer")]
//		static void Init() {
//			var window = EditorWindow.GetWindow(typeof(NarramancerEditorWindow));
//			window.Show();
//		}

//		[OnOpenAsset(0)]
//		public static bool OnOpen(int instanceID, int line) {
//			var map = EditorUtility.InstanceIDToObject(instanceID) as StoryScriptableObject;
//			if (map != null) {
//				Open(map);
//				return true;
//			}
//			return false;
//		}

//		public static NarramancerEditorWindow Open(StoryScriptableObject story) {
//			if (!story)
//				return null;

//			var allAssetMapWindows = Resources.FindObjectsOfTypeAll<NarramancerEditorWindow>();

//			var window = allAssetMapWindows.FirstOrDefault(x => x.story == story);

//			if (window == null) {
//				window = CreateWindow<NarramancerEditorWindow>(story.name);
//				window.story = story;
//			}
//			else {
//				window.Focus();
//			}

//			window.wantsMouseMove = true;
//			return window;
//		}


//		void OnGUI() {

//			var rect = new Rect(0, 0, position.width, position.height);
//			GUILayout.BeginArea(rect);

//			GUILayout.BeginVertical();

//			DrawToolBar();

//			//GUILayout.BeginHorizontal();

//			DrawAssetSelector();

//			//DrawResizeBar(ref resizeDraggingAsset, ref assetSelectorWidth);

//			//DrawSelectedInspector(inspectorWidth);

//			//DrawResizeBar(ref draggingInspector, ref inspectorWidth);

//			//DrawMiscWindow();

//			//GUILayout.EndHorizontal();

//			GUILayout.EndVertical();

//			GUILayout.EndArea();
//		}

//		void DrawToolBar() {
//			GUILayout.BeginHorizontal();

//			story = EditorGUILayout.ObjectField(story, typeof(StoryScriptableObject), false, GUILayout.ExpandWidth(false)) as StoryScriptableObject;

//			GUILayout.EndHorizontal();
//		}

//		Vector2 characterScrollPosition;
//		string characterSearch;
//		//NounScriptableObject[] filteredCharacters;
//		//NounScriptableObject selectedCharacter;

//		Vector2 adjectiveScrollPosition;
//		string adjectiveSearch;
//		//AdjectiveScriptableObject[] filteredAdjectives;
//		//AdjectiveScriptableObject selectedAdjectives;

//		Vector2 graphScrollPosition;
//		string graphSearch;
//		//NarramancerGraph[] filteredGraphs;
//		//NarramancerGraph selectedGraph;

//		void DrawAssetSelector() {
//			if (story == null) { return; }

//			GUILayout.BeginVertical();

//			GUILayout.Space(8);

//			tab = GUILayout.Toolbar(tab, new string[] { "Nouns", "Adjectives", "Graphs" });

//			GUILayout.BeginVertical("box");

//			switch (tab) {
//				case 0:
//					nounTypeFilter = GUILayout.Toolbar(nounTypeFilter, new string[] { "All", "Characters", "Items", "Locations" });
//					NounScriptableObject[] nouns = null;
//					switch (nounTypeFilter) {
//						default:
//						case 0:
//							nouns = story.Nouns.ToArray();
//							break;
//						case 1:
//							nouns = story.Nouns.Where(noun => noun.NounType == NounType.Character).ToArray();
//							break;
//						case 2:
//							nouns = story.Nouns.Where(noun => noun.NounType == NounType.Item).ToArray();
//							break;
//						case 3:
//							nouns = story.Nouns.Where(noun => noun.NounType == NounType.Location).ToArray();
//							break;
//					}
	
//					DrawSearchableListOfElements(ref characterSearch, nouns, ref characterScrollPosition);

//					if (GUILayout.Button("Create New...")) {
//						var path = EditorUtility.SaveFilePanelInProject("Create New Character", "Character", "asset", "Choose a directory and name", "Assets/Scriptable Objects/Characters");
//						if (path.IsNotNullOrEmpty()) {
//							var newCharacter = ScriptableObject.CreateInstance<NounScriptableObject>();
//							newCharacter.name = Path.GetFileNameWithoutExtension(path);
//							story.Nouns.Add(newCharacter);
//							EditorUtility.SetDirty(story);
//							AssetDatabase.CreateAsset(newCharacter, path);
//							AssetDatabase.Refresh();
//							AssetDatabase.SaveAssets();
//						}
//					}

//					break;
//				case 1:

//					if (GUILayout.Button("Add all used")) {
//						var newProperties = story.Nouns.SelectMany(noun => noun.Properties).Select(assignment => assignment.property);
//						var newStats = story.Nouns.SelectMany(noun => noun.Stats).Select(assignment => assignment.stat);
//						var newRelationships = story.Nouns.SelectMany(noun => noun.Relationships).Select(assignment => assignment.relationship);

//						story.Adjectives = story.Adjectives.Union(newProperties).Union(newStats).Union(newRelationships).WithoutNulls().ToList();
//					}

//					adjectiveTypeFilter = GUILayout.Toolbar(adjectiveTypeFilter, new string[] { "All", "Properties", "Stats", "Relationships" });
//					AdjectiveScriptableObject[] adjectives = null;
//					switch (adjectiveTypeFilter) {
//						default:
//						case 0:
//							adjectives = story.Adjectives.ToArray();
//							break;
//						case 1:
//							adjectives = story.Adjectives.OfType<PropertyScriptableObject>().WithoutNulls().ToArray();
//							break;
//						case 2:
//							adjectives = story.Adjectives.OfType<StatScriptableObject>().WithoutNulls().ToArray();
//							break;
//						case 3:
//							adjectives = story.Adjectives.OfType<RelationshipScriptableObject>().WithoutNulls().ToArray();
//							break;
//					}
//					DrawSearchableListOfElements(ref adjectiveSearch, adjectives, ref adjectiveScrollPosition);

//					break;
//				case 2:
//					DrawSearchableListOfElements(ref graphSearch, story.Graphs.ToArray(), ref graphScrollPosition);

//					break;
//			}
//			GUILayout.EndVertical();

//			GUILayout.EndVertical();


//			if (Event.current.type == EventType.DragUpdated) {
//				DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
//			}
//			if (Event.current.type == EventType.DragPerform) {
//				DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
//				var lastRect = GUILayoutUtility.GetLastRect();
//				if ( lastRect.Contains( Event.current.mousePosition)) {
//					DragAndDrop.AcceptDrag();
//					var selected = DragAndDrop.objectReferences;
//					foreach(NounScriptableObject noun in selected.Where(x=> x is NounScriptableObject)) {
//						if (!story.Nouns.Contains(noun)) {
//							story.Nouns.Add(noun);
//						}
//					}
//					foreach (AdjectiveScriptableObject adjective in selected.Where(x => x is AdjectiveScriptableObject)) {
//						if (!story.Adjectives.Contains(adjective)) {
//							story.Adjectives.Add(adjective);
//						}
//					}
//					foreach (VerbGraph graph in selected.Where(x => x is VerbGraph)) {
//						if (!story.Graphs.Contains(graph)) {
//							story.Graphs.Add(graph);
//						}
//					}
//					//filteredCharacters = null;
//					//filteredAdjectives = null;
//					//filteredGraphs = null;
//				}
				
//			}


			
//		}

//		void DrawSearchableListOfElements<T>(ref string search, T[] allValues, ref Vector2 scrollPosition) where T : UnityEngine.Object {
//			//EditorGUI.BeginChangeCheck();

//			EditorGUILayout.BeginHorizontal();
//			EditorGUILayout.LabelField(EditorGUIUtility.IconContent("d_Search Icon"), GUILayout.Width(20));
//			search = EditorGUILayout.TextField(search);

//			EditorGUILayout.EndHorizontal();

//			bool ContainsAnySearchTerms(UnityEngine.Object value, string[] terms) {
//				var fullName = value.name.ToLower();
//				if (terms.All(term => fullName.Contains(term))) {
//					return true;
//				}
//				return false;
//			}

//			var shownValues = allValues;

//			//if (EditorGUI.EndChangeCheck() || filteredValues == null) {

//				if (search.IsNotNullOrEmpty()) {
//				//	filteredValues = null;
//				//}
//				//else {
//					var searchLower = search.ToLower();
//					var searchTerms = searchLower.Split(' ');
//				shownValues = allValues.Where(type => ContainsAnySearchTerms(type, searchTerms)).ToArray();

//					scrollPosition.y = 0;
//				}
//			//}


//			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

//			var style = new GUIStyle(EditorStyles.objectField);
//			style.border = new RectOffset();
//			style.padding = new RectOffset();
//			//var style = new GUIStyle(GUI.skin.button);
//			//style.alignment = TextAnchor.UpperLeft;

//			var itemHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

//			var itemsAboveView = Mathf.Max(0, (int)(scrollPosition.y / itemHeight) - 1);

//			GUILayout.Space(itemsAboveView * itemHeight);

//			var itemsVisible = (int)(position.height / itemHeight) + 2;

//			EditorGUIUtility.SetIconSize(Vector2.one * 24);

//			for (int i = 0; i < itemsVisible && itemsAboveView + i < shownValues.Count(); i++) {
//				var element = shownValues[itemsAboveView + i];
//				var name = element.name;
//				//var tooltip = GetTooltip(element);
//				using (EditorDrawerUtilities.Color()) {
//					if (element == Selection.activeObject) {
//						GUI.color = new Color(0.6f, 0.6f, .99f);
//					}

//					if (elementContent==null) {
//						elementContent = EditorGUIUtility.IconContent("d_ScriptableObject Icon");
//					}
//					var content = new GUIContent(elementContent);
//					content.text = name;
//#if UNITY_2021_3_OR_NEWER
//					var customIcon = EditorGUIUtility.GetIconForObject(element);
//					if (customIcon != null) {
//						content.image = customIcon;
//					}
//#endif

//					if (GUILayout.Button(content, style)) {
//						EditorGUIUtility.PingObject(element);
//						Selection.activeObject = element;
//					}
//				}
//			}

//			EditorGUIUtility.SetIconSize(Vector2.zero);

//			var itemsBelowView = Mathf.Max(0, shownValues.Count() - itemsAboveView - itemsVisible);

//			if (itemsBelowView > 0) {
//				GUILayout.Space(itemsBelowView * itemHeight);
//			}
			

//			GUILayout.Space(20);

//			EditorGUILayout.EndScrollView();
//		}

//		//void DrawResizeBar(ref bool resizeDragging, ref float width) {
//		//	GUILayout.Label("", GUILayout.Width(8), GUILayout.ExpandHeight(true));
//		//	var resizeButtonRect = GUILayoutUtility.GetLastRect();

//		//	if (Event.current.type == EventType.MouseDown) {
//		//		if (resizeButtonRect.Contains(Event.current.mousePosition)) {
//		//			resizeDragging = true;
//		//			resizeDragStartPosition = Event.current.mousePosition.x;
//		//		}
//		//	}
//		//	if (resizeDragging) {
//		//		EditorGUI.DrawRect(resizeButtonRect, Color.blue);
//		//	}
//		//	else {
//		//		EditorGUI.DrawRect(resizeButtonRect, Color.black);
//		//	}

//		//	if (resizeDragging) {
//		//		if (Event.current.type == EventType.MouseUp) {
//		//			resizeDragging = false;
//		//		}
//		//		else {
//		//			var currentPosition = Event.current.mousePosition.x;
//		//			var delta = currentPosition - resizeDragStartPosition;
//		//			width += delta;
//		//			resizeDragStartPosition = currentPosition;
//		//			Repaint();
//		//		}
//		//	}
//		//}

//		//Editor characterEditor;
//		//Vector2 characterEditorScrollPosition;
//		//Editor modifierEditor;
//		//Vector2 modifierEditorScrollPosition;
//		//Editor graphEditor;
//		//Vector2 graphEditorScrollPosition;

//		//void DrawSelectedInspector(float width) {
//		//	GUILayout.BeginVertical("box", GUILayout.Width(width));

//		//	switch (tab) {
//		//		case 0:
//		//			if (selectedCharacter != null) {
//		//				characterEditorScrollPosition = EditorGUILayout.BeginScrollView(characterEditorScrollPosition);
//		//				Editor.CreateCachedEditor(selectedCharacter, null, ref characterEditor);
//		//				characterEditor.OnInspectorGUI();
//		//				GUILayout.Space(20);
//		//				EditorGUILayout.EndScrollView();
//		//			}
//		//			break;
//		//		case 1:
//		//			if (selectedAdjectives != null) {
//		//				modifierEditorScrollPosition = EditorGUILayout.BeginScrollView(modifierEditorScrollPosition);
//		//				Editor.CreateCachedEditor(selectedAdjectives, null, ref modifierEditor);
//		//				modifierEditor.OnInspectorGUI();
//		//				GUILayout.Space(20);
//		//				EditorGUILayout.EndScrollView();
//		//			}
//		//			break;
//		//		case 2:
//		//			if (selectedGraph != null) {
//		//				graphEditorScrollPosition = EditorGUILayout.BeginScrollView(graphEditorScrollPosition);
//		//				Editor.CreateCachedEditor(selectedGraph, typeof(NarramancerGraphInspector), ref graphEditor);
//		//				graphEditor.OnInspectorGUI();
//		//				GUILayout.Space(20);
//		//				EditorGUILayout.EndScrollView();
//		//			}
//		//			break;
//		//	}

//		//	GUILayout.EndVertical();
//		//}


//		//void DrawMiscWindow() {
//		//	GUILayout.BeginVertical("box");


//		//	GUILayout.EndVertical();
//		//}

//	}
//}