
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;

namespace Narramancer {
	public static class EditorDrawerUtilities {

		public static T GetTargetObject<T>(this SerializedProperty property) where T : class {
			var fieldInfo = property.GetFieldInfo();
			var targetObject = property.serializedObject.targetObject;
			var targetProperty = fieldInfo?.GetValue(targetObject);
			return targetProperty as T;
		}


		public static FieldInfo GetFieldInfo(this SerializedProperty property) {
			var targetObject = property.serializedObject.targetObject;
			var parentType = targetObject.GetType();
			var fieldInfo = parentType.GetField(property.propertyPath, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
			while (fieldInfo == null && parentType.BaseType != null) {
				parentType = parentType.BaseType;
				fieldInfo = parentType.GetField(property.propertyPath, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
			}
			return fieldInfo;
		}

		public static void RenameField(UnityEngine.Object targetObject, ref bool renaming) {
			if (!renaming) {
				if (GUILayout.Button("Rename")) {
					renaming = true;
				}
			}
			if (renaming) {
				EditorGUI.BeginChangeCheck();
				GUI.SetNextControlName(nameof(RenameField));
				var name = EditorGUILayout.DelayedTextField(targetObject.name);
				EditorGUI.FocusTextInControl(nameof(RenameField));
				if (EditorGUI.EndChangeCheck()) {
					if (name.IsNotNullOrEmpty()) {
						Undo.RecordObject(targetObject, "Rename");
						targetObject.name = name;
						EditorUtility.SetDirty(targetObject);
						AssetDatabase.SaveAssets();
						AssetDatabase.Refresh();
					}

					renaming = false;
				}
			}
		}

		public static void DuplciateNodeGraphField(UnityEngine.Object targetObject) {
			if (GUILayout.Button("Duplicate")) {
				var nodeGraph = targetObject as XNode.NodeGraph;
				var mainAssetPath = AssetDatabase.GetAssetPath(nodeGraph);
				var mainAsset = AssetDatabase.LoadMainAssetAtPath(mainAssetPath);
				Undo.RecordObject(mainAsset, "Duplicate");
				var duplicate = nodeGraph.Copy();
				AssetDatabase.AddObjectToAsset(duplicate, mainAssetPath);
				foreach (var node in duplicate.nodes) {
					AssetDatabase.AddObjectToAsset(node, mainAssetPath);
				}
				EditorUtility.SetDirty(mainAsset);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}
		}

		public static ColorScope Color(Color? color = null) {
			var scope = new ColorScope() {
				originalColor = GUI.color
			};
			if (color != null) {
				GUI.color = color.Value;
			}
			return scope;
		}

		public struct ColorScope : System.IDisposable {
			public Color originalColor;

			public void Dispose() {
				GUI.color = originalColor;
			}
		}


		private static readonly Type[] primitiveTypes = new Type[] {
			typeof(bool),
			typeof(int),
			typeof(float),
			typeof(string)
		};

		private static readonly Type[] narramancerTypes = new Type[] {
			typeof(NounInstance),
			typeof(NounScriptableObject),
			typeof(PropertyInstance),
			typeof(PropertyScriptableObject),
			typeof(StatInstance),
			typeof(StatScriptableObject),
			typeof(RelationshipInstance),
			typeof(RelationshipScriptableObject),
			typeof(SerializableSpawner),
		};

		private static readonly Type[] unityTypes = new Type[] {
			typeof(GameObject),
			typeof(Vector2),
			typeof(Vector3),
			typeof(Transform),
			typeof(RectTransform),
			typeof(ScriptableObject),
			typeof(Texture2D),
			typeof(Image),
			typeof(Sprite),
			typeof(Text),
			typeof(Rigidbody),
			typeof(Rigidbody2D)
		};

		public static void ShowTypeSelectionPopup(Action<Type> onTypeSelected, Action<GenericMenu> onBeforeTypeItems = null, Action<GenericMenu> onAfterTypeItems = null) {
			var mousePosition = Event.current.mousePosition;
			var screenPosition = GUIUtility.GUIToScreenPoint(mousePosition);

			GenericMenu context = new GenericMenu();

			onBeforeTypeItems?.Invoke(context);

			foreach (var type in primitiveTypes) {
				context.AddItem(new GUIContent("Primitive/" + type.Name), false, () => onTypeSelected(type));
			}

			foreach (var type in narramancerTypes) {
				context.AddItem(new GUIContent($"{nameof(Narramancer)}/" + type.Name), false, () => onTypeSelected(type));
			}

			var allTypes = AssemblyUtilities.GetAllPublicTypes().Where(type =>
					   !typeof(XNode.Node).IsAssignableFrom(type) &&
					   !typeof(Editor).IsAssignableFrom(type) &&
					   !typeof(XNodeEditor.NodeEditor).IsAssignableFrom(type) &&
					   !typeof(Attribute).IsAssignableFrom(type) &&
					   !typeof(PropertyDrawer).IsAssignableFrom(type)
					)
				.ToArray();


			foreach (var type in unityTypes) {
				context.AddItem(new GUIContent($"Unity Object/" + type.Name), false, () => onTypeSelected(type));
			}

			context.AddItem(new GUIContent("Unity Object/Search..."), false, () => {

				var allUnityObjects = new List<Type>();

				foreach (var type in allTypes) {
					if (typeof(UnityEngine.Object).IsAssignableFrom(type)) {
						allUnityObjects.Add(type);
					}
				}

				TypeSearchModalWindow window = ScriptableObject.CreateInstance(typeof(TypeSearchModalWindow)) as TypeSearchModalWindow;
				window.SearchTypes(screenPosition, allUnityObjects.ToArray(), onTypeSelected);
			});

			context.AddItem(new GUIContent("Search All..."), false, () => {

				TypeSearchModalWindow window = ScriptableObject.CreateInstance(typeof(TypeSearchModalWindow)) as TypeSearchModalWindow;
				window.SearchTypes(screenPosition, allTypes, onTypeSelected);
			});

			onAfterTypeItems?.Invoke(context);

			Matrix4x4 originalMatrix = GUI.matrix;
			GUI.matrix = Matrix4x4.identity;
			context.ShowAsContext();
			GUI.matrix = originalMatrix;
		}

		public static void OnReorderableListRemoveCallbackRemoveChildAsset(ReorderableList list) {
#if UNITY_2021_1_OR_NEWER
			var selectedIndices = list.selectedIndices.ToList();
			var selectedElements = selectedIndices.Select(index => list.serializedProperty.GetArrayElementAtIndex(index)).ToList();

			var path = AssetDatabase.GetAssetPath(list.serializedProperty.serializedObject.targetObject);
			var parent = AssetDatabase.LoadMainAssetAtPath(path);

			foreach (var element in selectedElements) {
				var assetObject = element.objectReferenceValue;

				if (assetObject && PseudoEditorUtilities.IsObjectAChildOfParent(assetObject, (ScriptableObject)parent)) {
					AssetDatabase.RemoveObjectFromAsset(assetObject);
					GameObject.DestroyImmediate(assetObject);
				}
			}

			foreach (var index in selectedIndices.OrderByDescending(i => i)) {
				list.serializedProperty.DeleteArrayElementAtIndex(index);
			}
#else
			var element = list.serializedProperty.GetArrayElementAtIndex(list.index);

			var path = AssetDatabase.GetAssetPath(list.serializedProperty.serializedObject.targetObject);
			var parent = AssetDatabase.LoadMainAssetAtPath(path);

			var assetObject = element.objectReferenceValue;

			if (assetObject && PseudoEditorUtilities.IsObjectAChildOfParent(assetObject, (ScriptableObject)parent)) {
			
				if ( assetObject is VerbGraph verbGraph) {
					verbGraph.Clear();
				}

				AssetDatabase.RemoveObjectFromAsset(assetObject);
				GameObject.DestroyImmediate(assetObject);
			}

			list.serializedProperty.DeleteArrayElementAtIndex(list.index);
#endif

			AssetDatabase.SaveAssets();
		}


		public static void VariableAssignmentField(Rect rect, SerializedProperty element, GUIContent label = null) {
			var name = element.FindPropertyRelative(nameof(VariableAssignment.name)).stringValue;
			label = label != null ? label : new GUIContent(name);

			var typeValue = element.FindPropertyRelative(nameof(VariableAssignment.type));
			switch (typeValue.stringValue) {
				case "int":
					var intValue = element.FindPropertyRelative(nameof(VariableAssignment.intValue));
					intValue.intValue = EditorGUI.IntField(rect, label, intValue.intValue);
					break;
				case "bool":
					var boolValue = element.FindPropertyRelative(nameof(VariableAssignment.boolValue));
					boolValue.boolValue = EditorGUI.Toggle(rect, label, boolValue.boolValue);
					break;
				case "float":
					var floatValue = element.FindPropertyRelative(nameof(VariableAssignment.floatValue));
					floatValue.floatValue = EditorGUI.FloatField(rect, label, floatValue.floatValue);
					break;
				case "string":
					var stringValue = element.FindPropertyRelative(nameof(VariableAssignment.stringValue));
					stringValue.stringValue = EditorGUI.TextField(rect, label, stringValue.stringValue);
					break;
				case "color":
					var colorValue = element.FindPropertyRelative(nameof(VariableAssignment.color));
					colorValue.colorValue = EditorGUI.ColorField(rect, label, colorValue.colorValue);
					break;
				case "vector2":
					var vector2Value = element.FindPropertyRelative(nameof(VariableAssignment.vector2));
					vector2Value.vector2Value = EditorGUI.Vector2Field(rect, label, vector2Value.vector2Value);
					break;
				case "vector3":
					var vector3Value = element.FindPropertyRelative(nameof(VariableAssignment.vector3));
					vector3Value.vector3Value = EditorGUI.Vector3Field(rect, label, vector3Value.vector3Value);
					break;
				default:
					var objectType = Type.GetType(typeValue.stringValue);
					var allowSceneObjects = typeof(GameObject).IsAssignableFrom(objectType) || typeof(Component).IsAssignableFrom(objectType);
					var objectValue = element.FindPropertyRelative(nameof(VariableAssignment.objectValue));

					objectValue.objectReferenceValue = EditorGUI.ObjectField(rect, label, objectValue.objectReferenceValue, objectType, allowSceneObjects);

					break;
			}
		}


		public static void DrawSearchableListOfUnityObjectsWithDragSupport<T>(ref string search, T[] allValues, ref UnityEngine.Object draggedElement, ref UnityEngine.Object lastHoveredElement) where T : UnityEngine.Object {


			if (Event.current.type == EventType.MouseDown) {
				draggedElement = lastHoveredElement;
			}

			if (Event.current.type == EventType.MouseDrag) {
				if (draggedElement != null) {
					DragAndDrop.PrepareStartDrag();
					DragAndDrop.StartDrag("Dragging " + draggedElement.name);
					DragAndDrop.objectReferences = new UnityEngine.Object[] { draggedElement };
					draggedElement = null;
				}
			}

			if (Event.current.type == EventType.MouseUp || Event.current.type == EventType.MouseLeaveWindow) {
				draggedElement = null;
			}

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(EditorGUIUtility.IconContent("d_Search Icon"), GUILayout.Width(20));
			search = EditorGUILayout.TextField(search);

			EditorGUILayout.EndHorizontal();

			bool ContainsAnySearchTerms(UnityEngine.Object value, string[] terms) {
				var fullName = value.name.ToLower();
				if (terms.All(term => fullName.Contains(term))) {
					return true;
				}
				return false;
			}

			var shownValues = allValues;

			if (search.IsNotNullOrEmpty()) {

				var searchLower = search.ToLower();
				var searchTerms = searchLower.Split(' ');
				shownValues = allValues.Where(type => ContainsAnySearchTerms(type, searchTerms)).ToArray();
			}

			var style = new GUIStyle(EditorStyles.objectField);
			style.border = new RectOffset();
			style.padding = new RectOffset();

			if (Event.current.type == EventType.Repaint) {
				lastHoveredElement = null;
			}

			EditorGUIUtility.SetIconSize(Vector2.one * 24);
			var content = new GUIContent(EditorGUIUtility.IconContent("d_ScriptableObject Icon"));

			foreach (var element in shownValues) {
				var name = element.name;
				//var tooltip = GetTooltip(element);
				using (EditorDrawerUtilities.Color()) {
					if (element == Selection.activeObject) {
						GUI.color = new Color(0.6f, 0.6f, .99f);
					}

					content.text = name;
#if UNITY_2021_3_OR_NEWER
					var customIcon = EditorGUIUtility.GetIconForObject(element);
					if (customIcon != null) {
						content.image = customIcon;
					}
#endif

					if (GUILayout.Button(content, style)) {
						EditorGUIUtility.PingObject(element);
						Selection.activeObject = element;

					}

					if (Event.current.type == EventType.Repaint) {
						var buttonRect = GUILayoutUtility.GetLastRect();

						if (buttonRect.Contains(Event.current.mousePosition)) {
							lastHoveredElement = element;
						}
					}

				}
			}

			EditorGUIUtility.SetIconSize(Vector2.zero);

		}
	}
}