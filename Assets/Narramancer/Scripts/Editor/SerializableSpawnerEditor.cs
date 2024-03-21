using System;
using UnityEditor;
using UnityEngine;

namespace Narramancer {
	[CustomEditor(typeof(SerializableSpawner))]
	public class SerializableSpawnerEditor : Editor {

		public override void OnInspectorGUI() {

			serializedObject.Update();

			using (new EditorGUI.DisabledScope(true)) {
				var script = serializedObject.FindProperty("m_Script");
				EditorGUILayout.PropertyField(script, true);
			}

			var prefabProperty = serializedObject.FindProperty(SerializableSpawner.PrefabFieldName);
			EditorGUILayout.PropertyField(prefabProperty);

			var spawnForEachNounProperty = serializedObject.FindProperty(SerializableSpawner.SpawnForEachNounFieldName);
			EditorGUILayout.PropertyField(spawnForEachNounProperty);

			var spawnLocationTypeProperty = serializedObject.FindProperty(SerializableSpawner.SpawnLocationTypeFieldName);
			EditorGUILayout.PropertyField(spawnLocationTypeProperty);

			switch ((SerializableSpawner.SpawnLocationType)spawnLocationTypeProperty.enumValueIndex) {
				case SerializableSpawner.SpawnLocationType.None:
					break;

				case SerializableSpawner.SpawnLocationType.AtTransform:
					var spawnLocationProperty = serializedObject.FindProperty(SerializableSpawner.SpawnLocationFieldName);
					EditorGUILayout.PropertyField(spawnLocationProperty);
					break;

				case SerializableSpawner.SpawnLocationType.RandomInXYCircle:
				case SerializableSpawner.SpawnLocationType.RandomInXZCircle:
				case SerializableSpawner.SpawnLocationType.RandomInYZCircle:
				case SerializableSpawner.SpawnLocationType.RandomInSphere:
					var circleRadiusProperty = serializedObject.FindProperty(SerializableSpawner.CircleRadiusFieldName);
					EditorGUILayout.PropertyField(circleRadiusProperty);
					break;

				case SerializableSpawner.SpawnLocationType.LayoutInLine:
					break;
				case SerializableSpawner.SpawnLocationType.RandomInXYRect:
				case SerializableSpawner.SpawnLocationType.RandomInXZRect:
				case SerializableSpawner.SpawnLocationType.RandomInYZRect:
					var rectWidthProperty = serializedObject.FindProperty(SerializableSpawner.RectWidthFieldName);
					EditorGUILayout.PropertyField(rectWidthProperty);
					var rectHeightProperty = serializedObject.FindProperty(SerializableSpawner.RectHeightFieldName);
					EditorGUILayout.PropertyField(rectHeightProperty);
					break;
			}

			var randomizeRotationProperty = serializedObject.FindProperty(SerializableSpawner.RandomizeRotationFieldName);
			EditorGUILayout.PropertyField(randomizeRotationProperty);

			serializedObject.ApplyModifiedProperties();
		}

	}
}