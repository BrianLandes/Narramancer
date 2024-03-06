
using System;
using UnityEditor;
using UnityEngine;
using XNodeEditor;

namespace Narramancer {
	[CustomNodeEditor(typeof(RunnableNode))]
	public class RunnableNodeEditor : NodeEditor {

		GUIStyle timeStyle;

		public override void OnHeaderGUI() {

			base.OnHeaderGUI();

			if (timeStyle == null) {
				timeStyle = new GUIStyle(EditorStyles.label);
				timeStyle.alignment = TextAnchor.UpperRight;
			}

			var lastNodeRunner = GetNodeRunner();

			if (lastNodeRunner != null) {

				var runnableNode = target as RunnableNode;
				float timeSinceLastRun = runnableNode.TimeSinceLastRun(lastNodeRunner);

				if (timeSinceLastRun > 0) {
					EditorGUILayout.Space(-30);

					TimeSpan timeSpan = TimeSpan.FromSeconds(timeSinceLastRun);

					string time;
					if (timeSpan.TotalMinutes > 1) {
						time = $"{timeSpan.TotalMinutes:0}m";
					}
					else {
						time = $"{timeSpan.TotalSeconds:0}s";
					}
					EditorGUILayout.LabelField(new GUIContent(time, $"last run {time} ago"), timeStyle, GUILayout.Height(30));
				}

			}
		}

		private NodeRunner GetNodeRunner() {
			if (Application.isPlaying) {
				return VerbGraphEditor.selectedNodeRunner;
			}
			return null;
		}

		public override void OnBodyGUI() {

			OnTopGUI();

			OnBaseBodyGUI();
		}

		public void OnBaseBodyGUI() {
			base.OnBodyGUI();
		}

		public virtual void OnTopGUI() {
			var runPort = target.GetInputPort(RunnableNode.ThisNodeField);
			NodeEditorGUILayout.PortField(runPort, serializedObject);
		}

		public override Color GetTint() {

			var runnableNode = target as RunnableNode;

			var lastNodeRunner = GetNodeRunner();

			if (lastNodeRunner != null && ColorUtility.TryParseHtmlString("#EF476F", out var color)) {

				float timeSinceLastRun = runnableNode.TimeSinceLastRun(lastNodeRunner);

				if (timeSinceLastRun > 6f) {
					var originalColor = base.GetTint();

					float fade = 1f - (10f - timeSinceLastRun) / 4f;

					return Color.Lerp(color, originalColor, fade);
				}

				if (timeSinceLastRun > 0f) {
					return color;
				}

			}

			return base.GetTint();
		}
	}
}