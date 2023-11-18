using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Narramancer {

	[CustomNodeEditor(typeof(GetFlagNode))]
	[CustomNodeEditor(typeof(IsFlagRaisedNode))]
	[CustomNodeEditor(typeof(RaiseFlagNode))]
	[CustomNodeEditor(typeof(RemoveFlagNode))]
	[CustomNodeEditor(typeof(SetFlagNode))]
	public class FlagNodeEditor : NodeEditor {

		public override void OnBodyGUI() {
			base.OnBodyGUI();

			var flagProperty = serializedObject.FindProperty("flag");
			if (flagProperty.objectReferenceValue == null) {
				if (GUILayout.Button("Create New Flag")) {

					var path = EditorUtility.SaveFilePanelInProject("Create New Flag", "New Flag", "asset", "Choose a save location for the new flag");
					if (path.IsNotNullOrEmpty()) {
						var flag = new Flag();
						AssetDatabase.CreateAsset(flag, path);
						EditorGUIUtility.PingObject(flag);

						serializedObject.Update();
						flagProperty.objectReferenceValue = flag;
						serializedObject.ApplyModifiedProperties();
					}
				}
			}
		}
	}
}