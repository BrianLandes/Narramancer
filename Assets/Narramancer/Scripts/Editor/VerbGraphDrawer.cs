
#if ODIN_INSPECTOR
using Sirenix.OdinInspector.Editor;
#endif
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEngine;
using XNode;
using XNodeEditor;

namespace Narramancer {
	[CustomPropertyDrawer(typeof(VerbGraph))]
	[CustomPropertyDrawer(typeof(ValueVerb))]
	[CustomPropertyDrawer(typeof(ActionVerb))]
	public class VerbGraphDrawer : PropertyDrawer {

#if ODIN_INSPECTOR
		PropertyTree objectTree;
#else
		Editor defaultEditor;
#endif
		StringBuilder stringBuilder;

		AnimBool showGraphInInspector;

		bool renaming = false;

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

			if (showGraphInInspector == null) {
				showGraphInInspector = new AnimBool(false);
				showGraphInInspector.valueChanged.AddListener(() => NodeEditorWindow.ForceRepaint = true);
			}

			EditorGUI.BeginProperty(position, label, property);

			if (!IsValid(property, out string errorMessage)) {
				var lines = Mathf.Max(2, errorMessage.Count(c => c == '\n'));
				var helpBoxPosition = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight * lines);
				EditorGUI.HelpBox(helpBoxPosition, errorMessage, MessageType.Warning);

				position = new Rect(position.x, position.y + helpBoxPosition.height, position.width, EditorGUIUtility.singleLineHeight);
			}

			position.height = EditorGUIUtility.singleLineHeight;

			var propertyAttributes = NodeEditorUtilities.GetCachedPropertyAttribs(property.serializedObject.targetObject.GetType(), property.name);
			var hideLabel = propertyAttributes.Any(attribute => attribute is HideLabelInNodeAttribute);
			var labelWidth = EditorGUIUtility.labelWidth;
			if (hideLabel) {
				label = GUIContent.none;
				labelWidth = 12f;
			}
			if (property.objectReferenceValue == null) {

#if ODIN_INSPECTOR
				objectTree = null;
#endif

				EditorGUI.BeginChangeCheck();
				EditorGUI.ObjectField(position, property, label);
				if (EditorGUI.EndChangeCheck()) {
					UpdateNodePorts(property);
				}

				var buttonPosition = new Rect(position.x + EditorGUIUtility.labelWidth, position.y + position.height + EditorGUIUtility.standardVerticalSpacing, position.width - EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
				if (GUI.Button(buttonPosition, "Create Graph")) {

					EditorGUI.EndProperty();

					var fieldInfo = property.GetFieldInfo();
					var fieldType = fieldInfo.FieldType;
					VerbGraph newVerbGraph = null;
					if (PseudoEditorUtilities.IsObjectAnAsset(property.serializedObject.targetObject)) {
						newVerbGraph = PseudoEditorUtilities.CreateAndAddChild(fieldType, property.serializedObject.targetObject, property.propertyPath.Nicify()) as VerbGraph;
					}
					//else
					//if (property.serializedObject.targetObject is MonoBehaviour monoBehaviour) {
					//	Undo.RecordObject(monoBehaviour.gameObject, "Create Verb");
					//	newVerbGraph = ScriptableObject.CreateInstance(fieldType) as VerbGraph;
					//	newVerbGraph.name = monoBehaviour.gameObject.name + " Verb";
					//	if (newVerbGraph is ActionVerb) {
					//		newVerbGraph.AddNode<RootNode>();
					//	}
					//}
					else {

						var path = EditorUtility.SaveFilePanelInProject("Choose a save location", "Verb Graph", "asset", "");
						if (path.IsNullOrEmpty()) {
							return;
						}
						newVerbGraph = ScriptableObject.CreateInstance(fieldType) as VerbGraph;
						AssetDatabase.CreateAsset(newVerbGraph, path);
						EditorUtility.SetDirty(newVerbGraph);
						AssetDatabase.SaveAssets();
						AssetDatabase.Refresh();
					}

					var variableNodePosition = new Vector2(-600, 0);
					if (newVerbGraph is ActionVerb) {
						variableNodePosition = new Vector2(0, 200);
					}

					foreach (var attribute in fieldInfo.GetCustomAttributes(false)) {

						if (attribute is RequireInputAttribute requireInputAttribute && requireInputAttribute.RequiredType != null) {
							var name = requireInputAttribute.DefaultName.IsNotNullOrEmpty() ? requireInputAttribute.DefaultName : requireInputAttribute.RequiredType.Name;
							var newInput = newVerbGraph.AddInput(requireInputAttribute.RequiredType, name);

							var variableNode = newVerbGraph.AddNode<GetVariableNode>(variableNodePosition) as GetVariableNode;
							variableNode.SetVariable(SerializableVariableReference.ScopeType.Verb, newInput);

							variableNodePosition.y += 100;
						}
						else
						if (attribute is RequireOutputAttribute requireOutputAttribute && requireOutputAttribute.RequiredType != null) {
							var name = requireOutputAttribute.DefaultName.IsNotNullOrEmpty() ? requireOutputAttribute.DefaultName : requireOutputAttribute.RequiredType.Name;
							newVerbGraph.AddOutput(requireOutputAttribute.RequiredType, name);

							// TODO: add SetVariableNodes for ActionVerb
						}
						else
						if (attribute is RequireInputFromSerializableTypeAttribute requireInputFromSerializableTypeAttribute) {
							var requiredType = GetTypeFromSerializableTypeField(property, requireInputFromSerializableTypeAttribute.TypeFieldName);
							if (requiredType != null) {
								var name = requireInputFromSerializableTypeAttribute.DefaultName.IsNotNullOrEmpty() ?
									requireInputFromSerializableTypeAttribute.DefaultName :
									requiredType.Name;
								var newInput = newVerbGraph.AddInput(requiredType, name);

								var variableNode = newVerbGraph.AddNode<GetVariableNode>(variableNodePosition) as GetVariableNode;
								variableNode.SetVariable(SerializableVariableReference.ScopeType.Verb, newInput);

								variableNodePosition.y += 100;
							}
						}
						else
						if (attribute is RequireOutputFromSerializableTypeAttribute requireOutputFromSerializableTypeAttribute) {
							var requiredType = GetTypeFromSerializableTypeField(property, requireOutputFromSerializableTypeAttribute.TypeFieldName);
							if (requiredType != null) {
								var name = requireOutputFromSerializableTypeAttribute.DefaultName.IsNotNullOrEmpty() ?
									requireOutputFromSerializableTypeAttribute.DefaultName :
									requiredType.Name;
								newVerbGraph.AddOutput(requiredType, name);

								// TODO: add SetGraphVariableNodes for RunnableGraph
							}
						}
					}

					property.serializedObject.Update();

					property.objectReferenceValue = newVerbGraph;

					UpdateNodePorts(property);

					property.serializedObject.ApplyModifiedProperties();

					EditorGUI.BeginProperty(position, label, property);
				}
			}
			else {

				var foldoutPosition = new Rect(position.x, position.y, labelWidth, EditorGUIUtility.singleLineHeight);
				property.isExpanded = EditorGUI.Foldout(foldoutPosition, property.isExpanded, label, true);

				EditorGUI.BeginChangeCheck();
				var objectPosition = new Rect(position.x + labelWidth, position.y, position.width - labelWidth, EditorGUIUtility.singleLineHeight);
				EditorGUI.ObjectField(objectPosition, property, GUIContent.none);
				if (EditorGUI.EndChangeCheck()) {
#if ODIN_INSPECTOR
					objectTree = null;
#endif
					UpdateNodePorts(property);
				}

			}
			if (property.objectReferenceValue!=null) {
				showGraphInInspector.target = property.isExpanded;
				if (EditorGUILayout.BeginFadeGroup(showGraphInInspector.faded)) {
					EditorGUILayout.BeginVertical("box");
					EditorGUI.indentLevel++;
					if (GUILayout.Button("Edit graph", GUILayout.Height(30))) {
						NodeEditorWindow.Open(property.objectReferenceValue as XNode.NodeGraph);
					}
					if (!AssetDatabase.IsMainAsset(property.objectReferenceValue)) {
						EditorDrawerUtilities.RenameField(property.objectReferenceValue, ref renaming);

						EditorDrawerUtilities.DuplicateNodeGraphField(property.objectReferenceValue);


					}
#if ODIN_INSPECTOR
					objectTree ??= PropertyTree.Create(property.objectReferenceValue);
					objectTree.BeginDraw(true);
					objectTree.Draw(true);
					objectTree.EndDraw();
#else
				Editor.CreateCachedEditor(property.objectReferenceValue, null, ref defaultEditor );
				defaultEditor.DrawDefaultInspector();
#endif

					EditorGUILayout.EndVertical();
					EditorGUI.indentLevel--;
				}
				EditorGUILayout.EndFadeGroup();
			}



			EditorGUI.EndProperty();
		}

		private Type GetTypeFromSerializableTypeField(SerializedProperty property, string fieldName) {
			var targetObject = property.serializedObject.targetObject;
			var parentType = targetObject.GetType();
			FieldInfo typeFieldInfo;
			if (fieldName.IsNotNullOrEmpty()) {
				typeFieldInfo = parentType.GetField(fieldName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
			}
			else {
				typeFieldInfo = parentType.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).FirstOrDefault(fieldInfo => fieldInfo.FieldType == typeof(SerializableType));
			}
			if (typeFieldInfo == null) {
				return null;
			}
			var serializableType = typeFieldInfo.GetValue(targetObject) as SerializableType;
			if (serializableType != null) {
				return serializableType.Type;
			}
			return null;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
			var lines = 1;
			if (!IsValid(property, out string errorMessage)) {
				lines += Mathf.Max(2, errorMessage.Count(c => c == '\n'));
			}
			if (property.objectReferenceValue == null) {
				lines += 1;
			}

			return EditorGUIUtility.singleLineHeight * lines + EditorGUIUtility.standardVerticalSpacing * (lines - 1);
		}

		bool IsValid(SerializedProperty property, out string errorMessage) {
			stringBuilder = stringBuilder!=null ? stringBuilder : new StringBuilder();
			stringBuilder.Clear();

			var fieldInfo = property.GetFieldInfo();
			var nodeGraph = property.objectReferenceValue as VerbGraph;
			foreach (var attribute in fieldInfo.GetCustomAttributes(false)) {

				if (attribute is VerbRequiredAttribute graphRequiresNotNullAttribute) {
					if (property.objectReferenceValue == null) {

						stringBuilder.AppendLine($"{property.propertyPath.Nicify()} must not null.");
					}
				}
				else
				if (attribute is RequireInputAttribute requireInputAttribute) {
					if (nodeGraph == null
						|| (requireInputAttribute.RequiredType!=null && !nodeGraph.HasInput(requireInputAttribute.RequiredType))
						|| (requireInputAttribute.RequiredType==null && !nodeGraph.Inputs.Any())
						) {
						var message = $"{property.propertyPath.Nicify()} must have an INPUT";
						if (requireInputAttribute.RequiredType!=null) {
							message += $" of type {requireInputAttribute.RequiredType.Name}";
						}
						if (requireInputAttribute.DefaultName.IsNotNullOrEmpty()) {
							message += $" for the {requireInputAttribute.DefaultName}";
						}
						message += ".";
						stringBuilder.AppendLine(message);
					}
				}
				else
				if (attribute is RequireOutputAttribute requireOutputAttribute) {
					if (nodeGraph == null
						|| (requireOutputAttribute.RequiredType != null && !nodeGraph.HasOutput(requireOutputAttribute.RequiredType))
						|| (requireOutputAttribute.RequiredType == null && !nodeGraph.Outputs.Any())
						) {
						var message = $"{property.propertyPath.Nicify()} must have an OUTPUT";
						if (requireOutputAttribute.RequiredType != null) {
							message += $" of type {requireOutputAttribute.RequiredType.Name}";
						}
						if (requireOutputAttribute.DefaultName.IsNotNullOrEmpty()) {
							message += $" for the {requireOutputAttribute.DefaultName}";
						}
						message += ".";
						stringBuilder.AppendLine(message);
					}
				}
				else
				if (attribute is RequireInputFromSerializableTypeAttribute requireInputFromSerializableTypeAttribute) {
					var requiredType = GetTypeFromSerializableTypeField(property, requireInputFromSerializableTypeAttribute.TypeFieldName);
					if (requiredType != null) {
						if (nodeGraph == null || !nodeGraph.HasInput(requiredType)) {
							if (requireInputFromSerializableTypeAttribute.DefaultName.IsNotNullOrEmpty()) {
								stringBuilder.AppendLine($"{property.propertyPath.Nicify()} must have an INPUT of type {requiredType.Name} for the {requireInputFromSerializableTypeAttribute.DefaultName}.");
							}
							else {
								stringBuilder.AppendLine($"{property.propertyPath.Nicify()} must have an INPUT of type {requiredType.Name}.");
							}
						}
					}
				}
				else
				if (attribute is RequireOutputFromSerializableTypeAttribute requireOutputFromSerializableTypeAttribute) {
					var requiredType = GetTypeFromSerializableTypeField(property, requireOutputFromSerializableTypeAttribute.TypeFieldName);
					if (requiredType != null) {
						if (nodeGraph == null || !nodeGraph.HasOutput(requiredType)) {
							if (requireOutputFromSerializableTypeAttribute.DefaultName.IsNotNullOrEmpty()) {
								stringBuilder.AppendLine($"{property.propertyPath.Nicify()} must have an INPUT of type {requiredType.Name} for the {requireOutputFromSerializableTypeAttribute.DefaultName}.");
							}
							else {
								stringBuilder.AppendLine($"{property.propertyPath.Nicify()} must have an INPUT of type {requiredType.Name}.");
							}
						}
					}
				}
			}

			if (property.objectReferenceValue != null) {
				var graph = property.GetTargetObject<VerbGraph>();

				var inputsOutputs = graph.Inputs.Union(graph.Outputs).ToList();

				if (inputsOutputs.Any(port => port.Type == null)) {
					stringBuilder.AppendLine($"All inputs and outputs must have a valid Type.");
				}

				var duplicateNames = inputsOutputs.GroupBy(port => port.Name).Where(group => group.Count() > 1).Select(group => group.Key).ToList();
				if (duplicateNames.Any()) {
					stringBuilder.AppendLine($"All inputs and outputs must have unique Names (two or more ports found with the following name(s):{duplicateNames.CommaSeparated()}).");
				}

			}

			errorMessage = stringBuilder.ToString();
			return errorMessage.IsNullOrEmpty();
		}

		private void UpdateNodePorts(SerializedProperty property) {
			var node = property.serializedObject.targetObject as Node;
			if (node != null) {
				property.serializedObject.ApplyModifiedProperties();
				node.UpdatePorts();
				property.serializedObject.Update();
			}
		}
	}
}