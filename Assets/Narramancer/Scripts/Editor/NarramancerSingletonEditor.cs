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

		ReorderableList verbList;
		ReorderableList variableList;

		private bool isInEditorMode = true; // vs runtime mode
		private int editorTab;
		private int nounTypeFilter = 0;
		private int adjectiveTypeFilter;

		string characterSearch;
		string adjectiveSearch;
		string verbSearch;

		bool openObjectPicker = false;
		const int RUN_AT_START_VERB_PICKER = 1 << 1;

		UnityEngine.Object lastHoveredElement;
		UnityEngine.Object draggedElement;

		[MenuItem("Window/Narramancer")]
		public static void OpenSceneProperties() {
			var narramancer = Resources.LoadAll<NarramancerSingleton>(string.Empty).FirstOrDefault();
			EditorUtility.OpenPropertyEditor(narramancer);
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

						if (singleton.Nouns.Any(x => x == null)) {
							foreach (var noun in singleton.Nouns.ToArray()) {
								if (noun == null) {
									singleton.Nouns.Remove(noun);
								}
							}
						}

						var buttonContent = new GUIContent( EditorGUIUtility.IconContent("CreateAddNew") );
						buttonContent.text = "Create new Noun...";
						if (GUILayout.Button(buttonContent)) {
							var path = EditorUtility.SaveFilePanelInProject("Create New Character", "Character", "asset", "Choose a directory and name", "Assets/Scriptable Objects/Characters");
							if (path.IsNotNullOrEmpty()) {
								var newCharacter = ScriptableObject.CreateInstance<NounScriptableObject>();
								newCharacter.name = Path.GetFileNameWithoutExtension(path);
								singleton.Nouns.Add(newCharacter);
								EditorUtility.SetDirty(singleton);
								AssetDatabase.CreateAsset(newCharacter, path);
								AssetDatabase.Refresh();
								AssetDatabase.SaveAssets();
							}
						}

						nounTypeFilter = GUILayout.Toolbar(nounTypeFilter, new string[] { "All", "Characters", "Items", "Locations" });
						NounScriptableObject[] nouns = null;

						switch (nounTypeFilter) {
							default:
							case 0:
								nouns = singleton.Nouns.ToArray();
								break;
							case 1:
								nouns = singleton.Nouns.Where(noun => noun.NounType == NounType.Character).ToArray();
								break;
							case 2:
								nouns = singleton.Nouns.Where(noun => noun.NounType == NounType.Item).ToArray();
								break;
							case 3:
								nouns = singleton.Nouns.Where(noun => noun.NounType == NounType.Location).ToArray();
								break;
						}

						EditorDrawerUtilities.DrawSearchableListOfUnityObjectsWithDragSupport(ref characterSearch, nouns, ref draggedElement, ref lastHoveredElement);

						break;
					#endregion

					#region Adjectives
					case 1:

						//if (GUILayout.Button("Add all used")) {
						//	var newProperties = story.Nouns.SelectMany(noun => noun.Properties).Select(assignment => assignment.property);
						//	var newStats = story.Nouns.SelectMany(noun => noun.Stats).Select(assignment => assignment.stat);
						//	var newRelationships = story.Nouns.SelectMany(noun => noun.Relationships).Select(assignment => assignment.relationship);

						//	story.Adjectives = story.Adjectives.Union(newProperties).Union(newStats).Union(newRelationships).WithoutNulls().ToList();
						//}

						//adjectiveTypeFilter = GUILayout.Toolbar(adjectiveTypeFilter, new string[] { "All", "Properties", "Stats", "Relationships" });
						//AdjectiveScriptableObject[] adjectives = null;
						//switch (adjectiveTypeFilter) {
						//	default:
						//	case 0:
						//		adjectives = story.Adjectives.ToArray();
						//		break;
						//	case 1:
						//		adjectives = story.Adjectives.OfType<PropertyScriptableObject>().WithoutNulls().ToArray();
						//		break;
						//	case 2:
						//		adjectives = story.Adjectives.OfType<StatScriptableObject>().WithoutNulls().ToArray();
						//		break;
						//	case 3:
						//		adjectives = story.Adjectives.OfType<RelationshipScriptableObject>().WithoutNulls().ToArray();
						//		break;
						//}
						//DrawSearchableListOfElements(ref adjectiveSearch, adjectives);

						break;
					#endregion

					#region Verbs
					case 2:

						var runAtStartProperty = serializedObject.FindProperty(NarramancerSingleton.RunAtStartFieldName);

						if (verbList == null) {
							verbList = new ReorderableList(serializedObject, runAtStartProperty, true, true, true, true);
							verbList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
								var element = verbList.serializedProperty.GetArrayElementAtIndex(index);
								EditorGUI.ObjectField(rect, element, GUIContent.none);
							};
							verbList.drawHeaderCallback = (rect) => {
								var name = runAtStartProperty.propertyPath.Nicify();
								EditorGUI.LabelField(rect, name);
							};
							verbList.onRemoveCallback = EditorDrawerUtilities.OnReorderableListRemoveCallbackRemoveChildAsset;
							verbList.onAddCallback = list => {
								var menu = new GenericMenu();

								menu.AddItem(new GUIContent("Create new child"), false, () => {
									serializedObject.Update();
									var newChildGraph = PseudoEditorUtilities.CreateAndAddChild(typeof(ActionVerb), serializedObject.targetObject, "Run on Start") as VerbGraph;
									runAtStartProperty = serializedObject.FindProperty(NarramancerSingleton.RunAtStartFieldName);
									runAtStartProperty.InsertArrayElementAtIndex(runAtStartProperty.arraySize);
									var newElement = runAtStartProperty.GetArrayElementAtIndex(runAtStartProperty.arraySize - 1);
									newElement.objectReferenceValue = newChildGraph;
									serializedObject.ApplyModifiedProperties();
								});

								menu.AddItem(new GUIContent("Add Existing"), false, () => {
									openObjectPicker = true;
									EditorGUIUtility.ShowObjectPicker<ActionVerb>(null, false, "", RUN_AT_START_VERB_PICKER);
								});

								menu.ShowAsContext();
							};

						}

						verbList.DoLayoutList();


						if (openObjectPicker && Event.current.commandName == "ObjectSelectorSelectionDone") {
							openObjectPicker = false;
							var selectedObject = EditorGUIUtility.GetObjectPickerObject();

							switch (EditorGUIUtility.GetObjectPickerControlID()) {
								case RUN_AT_START_VERB_PICKER:
									runAtStartProperty.arraySize++;
									var newVerb = runAtStartProperty.GetArrayElementAtIndex(runAtStartProperty.arraySize - 1);
									newVerb.objectReferenceValue = selectedObject;
									break;
							}

						}


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
					//foreach (AdjectiveScriptableObject adjective in selected.Where(x => x is AdjectiveScriptableObject)) {
					//	if (!singleton.Adjectives.Contains(adjective)) {
					//		singleton.Adjectives.Add(adjective);
					//	}
					//}
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