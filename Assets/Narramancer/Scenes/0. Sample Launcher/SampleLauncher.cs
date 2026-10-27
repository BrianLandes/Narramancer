using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Narramancer {

	/// <summary>
	/// Opens the sample scenes from inside a build, so one player can exercise all of them — the release smoke
	/// in <c>docs/RELEASE_RUNBOOK.md</c> otherwise means a build per scene.
	/// <para>
	/// Lists every other scene in Build Settings, so a new sample shows up by being added there. Survives scene
	/// loads and draws a small button inside each sample to come back. Drawn with IMGUI so it needs no canvas
	/// or prefab, and cannot collide with the samples' own UI.
	/// </para>
	/// </summary>
	public class SampleLauncher : MonoBehaviour {

		private static SampleLauncher instance;

		private int launcherBuildIndex;

		private readonly List<(int buildIndex, string name)> samples = new List<(int, string)>();

		private void Awake() {
			// Returning to the launcher scene loads a second copy of this object; the first one is still alive.
			if (instance != null) {
				Destroy(gameObject);
				return;
			}
			instance = this;

			// Read before DontDestroyOnLoad, which moves the object out of its scene.
			launcherBuildIndex = gameObject.scene.buildIndex;
			DontDestroyOnLoad(gameObject);

			for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++) {
				if (i == launcherBuildIndex) {
					continue;
				}
				var path = SceneUtility.GetScenePathByBuildIndex(i);
				// The samples live one folder each, and the folder name is the readable one ("1. Text and Choices Example").
				samples.Add((i, Path.GetFileName(Path.GetDirectoryName(path))));
			}

			SceneManager.sceneLoaded += OnSceneLoaded;
		}

		private void OnDestroy() {
			if (instance == this) {
				instance = null;
				SceneManager.sceneLoaded -= OnSceneLoaded;
			}
		}

		private void Start() {
			EnsureCamera();
		}

		private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
			if (scene.buildIndex == launcherBuildIndex) {
				EnsureCamera();
			}
		}

		/// <summary>The launcher scene has nothing to render, but without a camera the screen is never cleared.</summary>
		private static void EnsureCamera() {
			if (GameObjectExtensions.FindObjectsOfType<Camera>().Length > 0) {
				return;
			}
			var camera = new GameObject("Launcher Camera").AddComponent<Camera>();
			camera.clearFlags = CameraClearFlags.SolidColor;
			camera.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
		}

		private bool InLauncher => SceneManager.GetActiveScene().buildIndex == launcherBuildIndex;

		/// <summary>
		/// Starts a new story, then loads the scene. Without the reset the previous sample's nouns, blackboard and
		/// running verbs carry over — and those verbs keep ticking against objects the scene change destroyed.
		/// </summary>
		private void Open(int buildIndex) {
			NarramancerSingleton.Instance.StartNewStory();
			SceneManager.LoadScene(buildIndex);
		}

		private void OnGUI() {
			// Sized for 720p and scaled from there, so it stays legible on a high-DPI screen or a small WebGL canvas.
			var scale = Mathf.Max(1f, Screen.height / 720f);
			GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
			var width = Screen.width / scale;
			var height = Screen.height / scale;

			if (InLauncher) {
				DrawLauncher(width, height);
			}
			else
			if (GUI.Button(new Rect(10, height - 40, 110, 30), "◀ Samples")) {
				Open(launcherBuildIndex);
			}
		}

		private void DrawLauncher(float width, float height) {
			const float panelWidth = 360f;
			const float rowHeight = 40f;
			var panelHeight = 70f + samples.Count * (rowHeight + 8f);
			var panel = new Rect((width - panelWidth) / 2f, (height - panelHeight) / 2f, panelWidth, panelHeight);

			GUILayout.BeginArea(panel, GUI.skin.box);
			GUILayout.Label("Narramancer Samples", new GUIStyle(GUI.skin.label) {
				fontSize = 20,
				alignment = TextAnchor.MiddleCenter,
			}, GUILayout.Height(40));

			if (samples.Count == 0) {
				GUILayout.Label("No other scenes are in Build Settings.");
			}
			foreach (var (buildIndex, name) in samples) {
				if (GUILayout.Button(name, GUILayout.Height(rowHeight))) {
					Open(buildIndex);
				}
			}
			GUILayout.EndArea();
		}
	}
}
