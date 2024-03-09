
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Narramancer {
	[CustomPropertyDrawer(typeof(ActionVerbList))]
	public class ActionVerbListDrawer : PropertyDrawer {

		ReorderableList list;

		bool openObjectPicker = false;
		const int RUN_AT_START_VERB_PICKER = 1 << 1;

		private ReorderableList GetList(SerializedProperty property) {
			if (list == null) {
				var listProperty = property.FindPropertyRelative(nameof(ActionVerbList.list));
				list = new ReorderableList(property.serializedObject, listProperty, true, true, true, true);
				list.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
					var element = list.serializedProperty.GetArrayElementAtIndex(index);
					EditorGUI.ObjectField(rect, element, GUIContent.none);
				};
				list.drawHeaderCallback = (rect) => {
					var name = property.propertyPath.Nicify();
					EditorGUI.LabelField(rect, name);
				};
				list.onRemoveCallback = EditorDrawerUtilities.OnReorderableListRemoveCallbackRemoveChildAsset;
				list.onAddCallback = list => {
					var menu = new GenericMenu();

					menu.AddItem(new GUIContent("Create new asset..."), false, () => {

						var path = EditorUtility.SaveFilePanelInProject("Create New Action Verb", "Action Verb", "asset", "Choose a directory and name", "Assets/Scriptable Objects/Verbs");
						if (path.IsNotNullOrEmpty()) {
							var newVerb = ScriptableObject.CreateInstance<ActionVerb>();
							newVerb.name = Path.GetFileNameWithoutExtension(path);
							AssetDatabase.CreateAsset(newVerb, path);
							EditorUtility.SetDirty(property.serializedObject.targetObject);
							AssetDatabase.Refresh();
							AssetDatabase.SaveAssets();

							property.serializedObject.Update();
							listProperty.AddObject(newVerb);
							property.serializedObject.ApplyModifiedProperties();
						}

					});

					if (PseudoEditorUtilities.IsObjectAnAsset(property.serializedObject.targetObject)) {
						menu.AddItem(new GUIContent("Create new child"), false, () => {
							// TODO: name input dialog
							property.serializedObject.Update();
							var newChildGraph = PseudoEditorUtilities.CreateAndAddChild(typeof(ActionVerb), property.serializedObject.targetObject, "Action Verb") as VerbGraph;
							listProperty = property.FindPropertyRelative(nameof(ActionVerbList.list));
							listProperty.AddObject(newChildGraph);
							property.serializedObject.ApplyModifiedProperties();
						});
					}
#if NARRAMANCER_SCENE_GRAPH
// TODO: fix kinks with Scene Graphs
					else
					if (property.serializedObject.targetObject is MonoBehaviour monoBehaviour) {
						menu.AddItem(new GUIContent("Create new child"), false, () => {
							// TODO: name input dialog
							Undo.RecordObject(monoBehaviour.gameObject, "Create Verb");
							var newVerbGraph = ScriptableObject.CreateInstance< ActionVerb>();
							newVerbGraph.name = monoBehaviour.gameObject.name + " Verb";
							if (newVerbGraph is ActionVerb) {
								newVerbGraph.AddNode<RootNode>();
							}
							listProperty = property.FindPropertyRelative(nameof(ActionVerbList.list));
							listProperty.AddObject(newVerbGraph);
							property.serializedObject.ApplyModifiedProperties();
						});
					}
#endif

					menu.AddItem(new GUIContent("Add Existing..."), false, () => {
						openObjectPicker = true;
						EditorGUIUtility.ShowObjectPicker<ActionVerb>(null, false, "", RUN_AT_START_VERB_PICKER);
					});

					menu.ShowAsContext();
				};

			}
			return list;
		}



		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
			var list = GetList(property);

			property.serializedObject.Update();

			EditorGUI.BeginProperty(position, label, property);

			list.DoList(position);

			if (Event.current.type == EventType.Layout) {
				EditorGUILayout.Space(position.height);
			}

			var listProperty = property.FindPropertyRelative(nameof(ActionVerbList.list));

			if (openObjectPicker && Event.current.commandName == "ObjectSelectorSelectionDone") {
				openObjectPicker = false;
				var selectedObject = EditorGUIUtility.GetObjectPickerObject();

				switch (EditorGUIUtility.GetObjectPickerControlID()) {
					case RUN_AT_START_VERB_PICKER:
						listProperty.AddObject(selectedObject);
						break;
				}

			}

#region Accept DragAndDrop
			if (Event.current.type == EventType.DragUpdated) {
				DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
			}
			if (Event.current.type == EventType.DragPerform) {
				DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
				var lastRect = GUILayoutUtility.GetLastRect();
				if (lastRect.Contains(Event.current.mousePosition) && DragAndDrop.objectReferences.Any(@object => @object is ActionVerb)) {
					DragAndDrop.AcceptDrag();

					var selectedObjects = DragAndDrop.objectReferences.Where(@object => @object is ActionVerb);

					foreach (var selectedObject in selectedObjects) {
						listProperty.AddObject(selectedObject);
					}
				}

			}
#endregion

			property.serializedObject.ApplyModifiedProperties();

			EditorGUI.EndProperty();

		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
			var list = GetList(property);
			if (list != null) {

				return list.GetHeight();
			}
			return base.GetPropertyHeight(property, label);
		}

	}
}