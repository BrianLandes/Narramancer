using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Narramancer {

	[CustomNodeEditor(typeof(ChooseSpriteBasedOnFloatNode))]
	public class ChooseSpriteBasedOnFloatNodeEditor : NodeEditor {

		public override void OnBodyGUI() {

			serializedObject.Update();

			var valueProperty = serializedObject.FindProperty(ChooseSpriteBasedOnFloatNode.ValueFieldName);
			NodeEditorGUILayout.PropertyField(valueProperty);


			var pairings = serializedObject.FindProperty(ChooseSpriteBasedOnFloatNode.PairingsFieldName);

			pairings.isExpanded = EditorGUILayout.Foldout(pairings.isExpanded, "Pairings", true);
			if (pairings.isExpanded) {

				for (var ii = 0; ii < pairings.arraySize; ii++) {
					GUILayout.BeginVertical("box");
					var element = pairings.GetArrayElementAtIndex(ii);

					GUILayout.BeginHorizontal();
					var property = element.FindPropertyRelative(nameof(ChooseSpriteBasedOnFloatNode.Pairing.range));
					EditorGUILayout.PropertyField(property, GUIContent.none);

					if (GUILayout.Button("X", GUILayout.Width(20))) {
						var node = target as ChooseSpriteBasedOnFloatNode;
						node.RemovePairing(ii);
					}

					GUILayout.EndHorizontal();

					var nodePortName = element.FindPropertyRelative(nameof(ChooseSpriteBasedOnFloatNode.Pairing.portName));
					var nodePort = target.GetInputPort(nodePortName.stringValue);
					NodeEditorGUILayout.PortField(GUIContent.none, nodePort, serializedObject);

					GUILayout.EndVertical();

				}

				if (GUILayout.Button("Add Pairing")) {
					var node = target as ChooseSpriteBasedOnFloatNode;
					node.AddNewPairing();
				}

			}

			if (GUILayout.Button("Distribute Evenly")) {
				var node = target as ChooseSpriteBasedOnFloatNode;
				node.DistributeEvenly();
			}

			var defaultSprite = serializedObject.FindProperty(ChooseSpriteBasedOnFloatNode.DefaultSpriteFieldName);
			NodeEditorGUILayout.PropertyField(defaultSprite);

			var spriteOutputPort = target.GetOutputPort(ChooseSpriteBasedOnFloatNode.SpriteFieldName);
			NodeEditorGUILayout.PortField(spriteOutputPort, serializedObject);

			serializedObject.ApplyModifiedProperties();
		}
	}
}