using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Narramancer {

	[CustomNodeEditor(typeof(ChooseSpriteBasedOnPropertyNode))]
	public class ChooseSpriteBasedOnPropertyNodeEditor : AbstractInstanceInputNodeEditor {

		public override void OnBodyGUI() {
			OnInstanceInputGUI();

			serializedObject.Update();

			var pairings = serializedObject.FindProperty(ChooseSpriteBasedOnPropertyNode.PairingsFieldName);

			pairings.isExpanded = EditorGUILayout.Foldout(pairings.isExpanded, "Pairings", true);
			if (pairings.isExpanded) {

				for (var ii = 0; ii < pairings.arraySize; ii++) {
					GUILayout.BeginVertical("box");
					var element = pairings.GetArrayElementAtIndex(ii);

					GUILayout.BeginHorizontal();
					var property = element.FindPropertyRelative(nameof(ChooseSpriteBasedOnPropertyNode.Pairing.property));
					EditorGUILayout.PropertyField(property, GUIContent.none);

					if (GUILayout.Button("X", GUILayout.Width(20))) {
						var node = target as ChooseSpriteBasedOnPropertyNode;
						node.RemovePairing(ii);
					}

					GUILayout.EndHorizontal();

					var nodePortName = element.FindPropertyRelative(nameof(ChooseSpriteBasedOnPropertyNode.Pairing.portName));
					var nodePort = target.GetInputPort(nodePortName.stringValue);
					NodeEditorGUILayout.PortField(GUIContent.none, nodePort, serializedObject);

					GUILayout.EndVertical();

				}

				if (GUILayout.Button("Add Pairing")) {
					var node = target as ChooseSpriteBasedOnPropertyNode;
					node.AddNewPairing();
				}

			}

			var defaultSprite = serializedObject.FindProperty(ChooseSpriteBasedOnPropertyNode.DefaultSpriteFieldName);
			NodeEditorGUILayout.PropertyField(defaultSprite);

			var spriteOutputPort = target.GetOutputPort(ChooseSpriteBasedOnPropertyNode.SpriteFieldName);
			NodeEditorGUILayout.PortField(spriteOutputPort, serializedObject);

			serializedObject.ApplyModifiedProperties();
		}
	}
}