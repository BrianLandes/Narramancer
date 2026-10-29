
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UI;
using XNodeEditor;
using XNode;

namespace Narramancer {
	public static class EditorDrawerUtilities {

		private static readonly Regex arrayElementRegex = new Regex(@"^data\[(\d+)\]$");

		/// <summary>Where a <see cref="SerializedProperty"/> lands in the actual object graph.</summary>
		private struct PropertyLocation {
			/// <summary>The last <em>named</em> field on the path — for a list element, the list's field.</summary>
			public FieldInfo field;
			/// <summary>The object that declares <see cref="field"/>.</summary>
			public object parent;
			/// <summary>What the whole path points at: the field's value, or the list element.</summary>
			public object value;
			/// <summary>The declared type of <see cref="value"/> — for a list element, the element type.</summary>
			public Type valueType;
		}

		/// <summary>
		/// Walks a property path from the target object, segment by segment: through fields of nested
		/// serializable classes, into list and array elements (<c>items.Array.data[2]</c>), and up base classes
		/// for private fields. Each step looks fields up on the <em>runtime</em> type of the object it is in, so a
		/// <c>[SerializeReference]</c> subclass resolves its own fields.
		/// <para>
		/// The helpers this replaced only understood a top-level field or one <c>list.Array.data[i].field</c>
		/// hop, so any drawer used one level deeper silently resolved nothing.
		/// </para>
		/// </summary>
		private static bool TryLocate(SerializedProperty property, out PropertyLocation location) {
			location = default;
			object current = property.serializedObject.targetObject;
			var currentType = current.GetType();
			var parts = property.propertyPath.Split('.');

			for (var ii = 0; ii < parts.Length; ii++) {
				if (parts[ii] == "Array") {
					var element = ii + 1 < parts.Length ? arrayElementRegex.Match(parts[ii + 1]) : Match.Empty;
					if (!element.Success) {
						// "Array.size" — the list itself is as far as a field path goes.
						return location.field != null;
					}
					ii++;
					var index = int.Parse(element.Groups[1].Value);
					var list = current as System.Collections.IList;
					current = list != null && index < list.Count ? list[index] : null;
					location.value = current;
					location.valueType = ElementType(currentType);
					currentType = current?.GetType() ?? location.valueType;
					continue;
				}

				var owner = current;
				var field = (owner?.GetType() ?? currentType).GetFieldInfo(parts[ii]);
				if (field == null) {
					return false;
				}
				current = owner == null ? null : field.GetValue(owner);
				currentType = current?.GetType() ?? field.FieldType;
				location.field = field;
				location.parent = owner;
				location.value = current;
				location.valueType = field.FieldType;
			}
			return location.field != null;
		}

		private static Type ElementType(Type listType) {
			if (listType.IsArray) {
				return listType.GetElementType();
			}
			if (listType.IsGenericType) {
				return listType.GetGenericArguments()[0];
			}
			return typeof(object);
		}

		/// <summary>The object the property points at, or null if it is not a <typeparamref name="T"/>.</summary>
		public static T GetTargetObject<T>(this SerializedProperty property) where T : class {
			return TryLocate(property, out var location) ? location.value as T : null;
		}

		/// <summary>The field the property is drawn from — for a list element, the list's field.</summary>
		public static FieldInfo GetFieldInfo(this SerializedProperty property) {
			return TryLocate(property, out var location) ? location.field : null;
		}

		/// <summary>The declared type of what the property points at — for a list element, the element type.</summary>
		public static Type GetPropertyTargetType(this SerializedProperty property) {
			return TryLocate(property, out var location) ? location.valueType : null;
		}

		/// <summary>The object that declares the property's field: the target object, or a nested one.</summary>
		public static object GetTargetObjectParent(this SerializedProperty property) {
			return TryLocate(property, out var location) ? location.parent : null;
		}

		/// <summary>The runtime type of <see cref="GetTargetObjectParent"/>, which is where its attributes live.</summary>
		public static Type GetParentType(this SerializedProperty property) {
			return TryLocate(property, out var location) ? location.parent?.GetType() ?? location.field.DeclaringType : null;
		}

		/// <summary>The name of the first field on <paramref name="targetObject"/> holding a <typeparamref name="T"/>, or null.</summary>
		public static string GetFirstFieldWithType<T>(this object targetObject) {
			for (var type = targetObject.GetType(); type != null; type = type.BaseType) {
				foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)) {
					if (typeof(T).IsAssignableFrom(field.FieldType)) {
						return field.Name;
					}
				}
			}
			return null;
		}

		public static FieldInfo GetFieldInfo(this Type type, string fieldName) {

			var fieldInfo = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
			while (fieldInfo == null && type.BaseType != null) {
				type = type.BaseType;
				fieldInfo = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
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

		public static void DuplicateNodeGraphField(UnityEngine.Object targetObject) {
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

		public static void ExtractChildNodeGraphField(UnityEngine.Object targetObject) {
			if (GUILayout.Button("Extract into Asset")) {
				var nodeGraph = targetObject as XNode.NodeGraph;
				var path = EditorUtility.SaveFilePanelInProject("Save child asset as main asset", nodeGraph.name, "asset", "Choose a folder and name for the new asset");
				if (path.IsNotNullOrEmpty()) {

					AssetDatabase.RemoveObjectFromAsset(nodeGraph);
					AssetDatabase.CreateAsset(nodeGraph, path);

					foreach (var node in nodeGraph.nodes) {
						AssetDatabase.RemoveObjectFromAsset(node);
						AssetDatabase.AddObjectToAsset(node, path);
					}
					EditorUtility.SetDirty(nodeGraph);
					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh();
				}

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

		public static WideLabelScope WideLabel(float? labelWidth = null) {
			var scope = new WideLabelScope() {
				originalWidth = EditorGUIUtility.labelWidth
			};
			if (labelWidth != null) {
				EditorGUIUtility.labelWidth = labelWidth.Value;
			}
			else {
				EditorGUIUtility.labelWidth = 160f;
			}
			return scope;
		}

		public struct WideLabelScope : System.IDisposable {
			public float originalWidth;

			public void Dispose() {
				EditorGUIUtility.labelWidth = originalWidth;
			}
		}


		public static readonly Dictionary<Type, string> primitiveTypes = new Dictionary<Type, string> {
			{ typeof(bool), "bool" },
			{ typeof(int), "int" },
			{ typeof(float), "float" },
			{ typeof(string), "string" },
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

		public static void ShowTypeSelectionPopup(Action<Type> onTypeSelected, Action<GenericMenu> onBeforeTypeItems = null, Action<GenericMenu> onAfterTypeItems = null, Func<Type, bool> typeFilter = null, Type selectedType = null) {
			var mousePosition = Event.current.mousePosition;
			var screenPosition = GUIUtility.GUIToScreenPoint(mousePosition);

			GenericMenu context = new GenericMenu();

			onBeforeTypeItems?.Invoke(context);

			foreach (var pair in primitiveTypes) {
				if (typeFilter == null || typeFilter(pair.Key)) {
					context.AddItem(new GUIContent("Primitive/" + pair.Value), selectedType == pair.Key, () => onTypeSelected(pair.Key));
				}
			}

			foreach (var type in narramancerTypes) {
				if (typeFilter == null || typeFilter(type)) {
					context.AddItem(new GUIContent($"{nameof(Narramancer)}/" + type.Name), selectedType == type, () => onTypeSelected(type));
				}
			}

			var allTypes = AssemblyUtilities.GetAllPublicTypes().Where(type =>
					   !typeof(XNode.Node).IsAssignableFrom(type) &&
					   !typeof(Editor).IsAssignableFrom(type) &&
					   !typeof(XNodeEditor.NodeEditor).IsAssignableFrom(type) &&
					   !typeof(Attribute).IsAssignableFrom(type) &&
					   !typeof(PropertyDrawer).IsAssignableFrom(type)
					)
				.Where(type => typeFilter == null || typeFilter(type))
				.ToArray();


			var filteredUnityTypes = typeFilter == null ? unityTypes : unityTypes.Where(x => typeFilter(x)).ToArray();

			if (filteredUnityTypes.Any()) {
				foreach (var type in filteredUnityTypes) {
					context.AddItem(new GUIContent($"Unity Object/" + type.Name), selectedType == type, () => onTypeSelected(type));
				}

				context.AddItem(new GUIContent("Unity Object/Search..."), false, () => {

					var allUnityObjects = new List<Type>();

					foreach (var type in allTypes) {
						if (typeFilter == null || typeFilter(type)) {
							if (typeof(UnityEngine.Object).IsAssignableFrom(type) || (type.Namespace.IsNotNullOrEmpty() && type.Namespace.Contains("Unity"))) {
								allUnityObjects.Add(type);
							}
						}
					}

					TypeSearchModalWindow window = ScriptableObject.CreateInstance(typeof(TypeSearchModalWindow)) as TypeSearchModalWindow;
					window.SearchTypes(screenPosition, allUnityObjects.ToArray(), onTypeSelected);
				});
			}

			if (typeFilter != null && allTypes.Count() < 20) {
				foreach (var type in allTypes) {
					context.AddItem(new GUIContent(type.Name), selectedType == type, () => onTypeSelected(type));
				}
			}

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


		public static SerializedProperty CreateNewElement(this SerializedProperty listProperty) {
			listProperty.InsertArrayElementAtIndex(listProperty.arraySize);
			return listProperty.GetArrayElementAtIndex(listProperty.arraySize - 1);
		}

		public static SerializedProperty AddObject(this SerializedProperty listProperty, UnityEngine.Object @object) {
			var newElement = listProperty.CreateNewElement();
			newElement.objectReferenceValue = @object;
			return newElement;
		}

		public static void ShowSearchNodesPopup(NodeGraph nodeGraph, Vector2 mousePosition) {
			Vector2 windowMousePosition = NodeEditorWindow.current.WindowToGridPosition(mousePosition);
			var screenPosition = GUIUtility.GUIToScreenPoint(mousePosition);
			ShowSearchNodesPopup(nodeGraph, windowMousePosition, screenPosition);
		}

		/// <summary>Shows the popup at <paramref name="screenPosition"/>, and creates the chosen node at <paramref name="gridPosition"/>.</summary>
		public static void ShowSearchNodesPopup(NodeGraph nodeGraph, Vector2 gridPosition, Vector2 screenPosition) {
			NodeSearchModalWindow window = ScriptableObject.CreateInstance(typeof(NodeSearchModalWindow)) as NodeSearchModalWindow;
			var allowRunnableNodes = typeof(ActionVerb).IsAssignableFrom(nodeGraph.GetType());
			window.ShowForValues(screenPosition, allowRunnableNodes, type => {
				XNode.Node node = nodeGraph.CreateNode(type, gridPosition);
				NodeEditorWindow.current.AutoConnect(node);
			});
		}
	}
}