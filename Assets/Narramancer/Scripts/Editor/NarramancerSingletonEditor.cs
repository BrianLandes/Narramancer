using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Narramancer {
	[CustomEditor(typeof(NarramancerSingleton))]
	public class NarramancerSingletonEditor : Editor {

		ReorderableList variableList;

		private bool isInEditorMode = true; // vs runtime mode
		private int editorTab;
		private int adjectiveTypeFilter;

		string adjectiveSearch;

		UnityEngine.Object lastHoveredElement;
		UnityEngine.Object draggedElement;

		[MenuItem("Window/Narramancer")]
		public static void OpenWindow() {
			var narramancer = Resources.LoadAll<NarramancerSingleton>(string.Empty).FirstOrDefault();
			if (narramancer == null) {
				narramancer = CreateSingletonInResources();
			}
#if UNITY_2021_1_OR_NEWER
			EditorUtility.OpenPropertyEditor(narramancer);
#else
			Selection.activeObject = narramancer;
			EditorGUIUtility.PingObject(narramancer);
#endif
		}

		public static NarramancerSingleton CreateSingletonInResources(string resourcesPath = "Assets/Resources") {
			var narramancer = ScriptableObject.CreateInstance<NarramancerSingleton>();
			narramancer.name = "Narramancer";
			var directoryAbsolutePath = PathUtilities.AsAbsolutePath(resourcesPath);
			Directory.CreateDirectory(directoryAbsolutePath);
			var path = PathUtilities.CreateNewAssetPath(resourcesPath, narramancer.name);
			AssetDatabase.CreateAsset(narramancer, path);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
			return narramancer;
		}

		public override void OnInspectorGUI() {

			serializedObject.Update();

			var singleton = target as NarramancerSingleton;

			using (new EditorGUI.DisabledScope(true)) {
				var script = serializedObject.FindProperty("m_Script");
				EditorGUILayout.PropertyField(script, true);
			}

			GUILayout.Space(8);

#region Editor or Runtime Selector

			GUILayout.BeginHorizontal();

			//if (!Application.isPlaying) {
			//	isInEditorMode = true;
			//}

			using (EditorDrawerUtilities.Color(isInEditorMode ? GUI.skin.settings.selectionColor : GUI.color )) {
				if (GUILayout.Button("Editor")) {
					isInEditorMode = true;
				}
			}
				

			//using (new EditorGUI.DisabledScope(!Application.isPlaying)) {
				using (EditorDrawerUtilities.Color(!isInEditorMode ? GUI.skin.settings.selectionColor : GUI.color)) {
					if (GUILayout.Button("Runtime")) {
						isInEditorMode = false;
					}
				}
			//}

			GUILayout.EndHorizontal();
#endregion

#region Draw Editor Assets

			if (isInEditorMode) {

				GUILayout.BeginVertical();

				GUILayout.Space(8);

				editorTab = GUILayout.Toolbar(editorTab, new string[] { "Nouns", "Adjectives", "Verbs", "Variables" });

				GUILayout.BeginVertical("box");

				switch (editorTab) {
#region Nouns
					case 0:

						var nounsProperty = serializedObject.FindProperty(NarramancerSingleton.NounsFieldName);
						EditorGUILayout.PropertyField(nounsProperty);

						break;
#endregion

#region Adjectives
					case 1:

						if (singleton.Adjectives.Any(x => x == null)) {
							foreach (var value in singleton.Adjectives.ToArray()) {
								if (value == null) {
									singleton.Adjectives.Remove(value);
								}
							}
						}

						var buttonContent2 = new GUIContent(EditorGUIUtility.IconContent("CreateAddNew"));
						buttonContent2.text = "Create new Adjective...";
						if (GUILayout.Button(buttonContent2)) {

							var menu = new GenericMenu();

							menu.AddItem(new GUIContent("Property"), false, () => {
								var path = EditorUtility.SaveFilePanelInProject("Create New Property", "Property", "asset", "Choose a directory and name", "Assets/Scriptable Objects/Properties");
								if (path.IsNotNullOrEmpty()) {
									var newProperty = ScriptableObject.CreateInstance<PropertyScriptableObject>();
									newProperty.name = Path.GetFileNameWithoutExtension(path);
									singleton.Adjectives.Add(newProperty);
									EditorUtility.SetDirty(singleton);
									AssetDatabase.CreateAsset(newProperty, path);
									AssetDatabase.Refresh();
									AssetDatabase.SaveAssets();
								}
							});

							menu.AddItem(new GUIContent("Stat"), false, () => {
								var path = EditorUtility.SaveFilePanelInProject("Create New Stat", "Stat", "asset", "Choose a directory and name", "Assets/Scriptable Objects/Stats");
								if (path.IsNotNullOrEmpty()) {
									var newStat = ScriptableObject.CreateInstance<StatScriptableObject>();
									newStat.name = Path.GetFileNameWithoutExtension(path);
									singleton.Adjectives.Add(newStat);
									EditorUtility.SetDirty(singleton);
									AssetDatabase.CreateAsset(newStat, path);
									AssetDatabase.Refresh();
									AssetDatabase.SaveAssets();
								}
							});

							menu.AddItem(new GUIContent("Relationship"), false, () => {
								var path = EditorUtility.SaveFilePanelInProject("Create New Relationship", "Relationship", "asset", "Choose a directory and name", "Assets/Scriptable Objects/Relationships");
								if (path.IsNotNullOrEmpty()) {
									var newRelationship = ScriptableObject.CreateInstance<RelationshipScriptableObject>();
									newRelationship.name = Path.GetFileNameWithoutExtension(path);
									singleton.Adjectives.Add(newRelationship);
									EditorUtility.SetDirty(singleton);
									AssetDatabase.CreateAsset(newRelationship, path);
									AssetDatabase.Refresh();
									AssetDatabase.SaveAssets();
								}
							});

							menu.ShowAsContext();
						}

						if (GUILayout.Button("Add all used")) {
							var newProperties = singleton.Nouns.SelectMany(noun => noun.Properties).Select(assignment => assignment.property);
							var newStats = singleton.Nouns.SelectMany(noun => noun.Stats).Select(assignment => assignment.stat);
							var newRelationships = singleton.Nouns.SelectMany(noun => noun.Relationships).Select(assignment => assignment.relationship);

							singleton.Adjectives = singleton.Adjectives.Union(newProperties).Union(newStats).Union(newRelationships).WithoutNulls().ToList();
						}

						adjectiveTypeFilter = GUILayout.Toolbar(adjectiveTypeFilter, new string[] { "All", "Properties", "Stats", "Relationships" });
						AdjectiveScriptableObject[] adjectives = null;
						switch (adjectiveTypeFilter) {
							default:
							case 0:
								adjectives = singleton.Adjectives.ToArray();
								break;
							case 1:
								adjectives = singleton.Adjectives.OfType<PropertyScriptableObject>().WithoutNulls().ToArray();
								break;
							case 2:
								adjectives = singleton.Adjectives.OfType<StatScriptableObject>().WithoutNulls().ToArray();
								break;
							case 3:
								adjectives = singleton.Adjectives.OfType<RelationshipScriptableObject>().WithoutNulls().ToArray();
								break;
						}
						EditorDrawerUtilities.DrawSearchableListOfUnityObjectsWithDragSupport(ref adjectiveSearch, adjectives, ref draggedElement, ref lastHoveredElement);

						break;
#endregion

#region Verbs
					case 2:

						var runAtStartProperty = serializedObject.FindProperty(NarramancerSingleton.RunAtStartFieldName);
						EditorGUILayout.PropertyField(runAtStartProperty);
						
						break;
#endregion

#region Variables
					case 3:

						var globalVariablesProperty = serializedObject.FindProperty(NarramancerSingleton.GlobalVariablesFieldName);

						if (variableList == null) {
							variableList = new ReorderableList(serializedObject, globalVariablesProperty, true, true, true, true);
							variableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
								var element = variableList.serializedProperty.GetArrayElementAtIndex(index);
								EditorGUI.PropertyField(rect, element, GUIContent.none);
							};
							variableList.headerHeight = EditorGUIUtility.singleLineHeight * 2f;
							variableList.drawHeaderCallback = (rect) => {
								var name = globalVariablesProperty.propertyPath.Nicify();
								var headerRect = new Rect(rect.x, rect.y, rect.width, rect.height * 0.5f);
								EditorGUI.LabelField(headerRect, name);

								var style = new GUIStyle(GUI.skin.label);
								style.alignment = TextAnchor.LowerCenter;
								style.fontSize = 12;

								var typeRect = new Rect(rect.x, rect.y + headerRect.height, rect.width * 0.3f, headerRect.height);
								EditorGUI.LabelField(typeRect, "Type", style);

								var nameRect = new Rect(rect.x + rect.width * 0.3f, rect.y + headerRect.height, rect.width * 0.4f, headerRect.height);
								EditorGUI.LabelField(nameRect, "Name", style);

								var valueRect = new Rect(rect.x + rect.width * 0.7f, rect.y + headerRect.height, rect.width * 0.3f, headerRect.height);
								EditorGUI.LabelField(valueRect, "Starting Value", style);
							};

							variableList.onAddCallback = list => {
								EditorDrawerUtilities.ShowTypeSelectionPopup(type => {
									globalVariablesProperty.InsertArrayElementAtIndex(globalVariablesProperty.arraySize);
									var newElement = globalVariablesProperty.GetArrayElementAtIndex(globalVariablesProperty.arraySize - 1);
									var typeProperty = newElement.FindPropertyRelative(NarramancerPort.TypeFieldName);
									var typeTypeProperty = typeProperty.FindPropertyRelative(SerializableType.TypeFieldName);
									typeTypeProperty.stringValue = type.AssemblyQualifiedName;
									var nameProperty = newElement.FindPropertyRelative(NarramancerPort.NameFieldName);
									nameProperty.stringValue = type.Name.Uncapitalize();
									serializedObject.ApplyModifiedProperties();
								});

							};

						}

						variableList.DoLayoutList();


						singleton.GlobalVariables.EnsurePortsHaveUniqueIds();


						break;
#endregion
				}
				GUILayout.EndVertical();

				GUILayout.EndVertical();
			}
#endregion Draw Editor Assets

#region Draw Runtime Assets

			if (!isInEditorMode) {

				GUILayout.BeginVertical();

				GUILayout.Space(8);

				var storyProperty = serializedObject.FindProperty(NarramancerSingleton.StoryInstanceFieldName);

				var instancesProperty = storyProperty.FindPropertyRelative(StoryInstance.InstancesFieldName);
				EditorGUILayout.PropertyField(instancesProperty);

				var blackboardProperty = storyProperty.FindPropertyRelative(StoryInstance.BlackboardFieldName);
				EditorGUILayout.PropertyField(blackboardProperty);

				GUILayout.EndVertical();
			}

#endregion

#region Accept DragAndDrop
			if (Event.current.type == EventType.DragUpdated) {
				DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
			}
			if (Event.current.type == EventType.DragPerform) {
				DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
				var lastRect = GUILayoutUtility.GetLastRect();
				if (lastRect.Contains(Event.current.mousePosition)) {
					DragAndDrop.AcceptDrag();
					var selected = DragAndDrop.objectReferences;
					foreach (NounScriptableObject noun in selected.Where(x => x is NounScriptableObject)) {
						if (!singleton.Nouns.Contains(noun)) {
							singleton.Nouns.Add(noun);
						}
					}
					foreach (AdjectiveScriptableObject adjective in selected.Where(x => x is AdjectiveScriptableObject)) {
						if (!singleton.Adjectives.Contains(adjective)) {
							singleton.Adjectives.Add(adjective);
						}
					}
					//foreach (VerbGraph graph in selected.Where(x => x is VerbGraph)) {
					//	if (!singleton.Graphs.Contains(graph)) {
					//		singleton.Graphs.Add(graph);
					//	}
					//}
				}

			}
#endregion

			serializedObject.ApplyModifiedProperties();
		}

	}
}