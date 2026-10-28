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
			camera.backgroundColor = Navy;
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

		#region Drawing

		// The icon's own colours, so the launcher reads as the same product as the editor and the listing.
		private static readonly Color Navy = new Color32(0x0F, 0x21, 0x30, 0xFF);
		private static readonly Color Panel = new Color32(0x16, 0x22, 0x32, 0xFF);
		private static readonly Color Row = new Color32(0x1E, 0x2E, 0x42, 0xFF);
		private static readonly Color Slate = new Color32(0xAE, 0xBE, 0xCF, 0xFF);
		private static readonly Color Accent = new Color32(0xEF, 0x47, 0x6F, 0xFF);

		[SerializeField]
		[Tooltip("Drawn above the list. Optional.")]
		private Texture2D icon = default;

		private GUIStyle titleStyle, subtitleStyle, rowStyle, numberStyle, backStyle;

		private void OnGUI() {
			EnsureStyles();

			// Sized for 720p and scaled up from there, so it stays legible on a high-DPI screen — and scaled down when
			// the list would not fit, as on a short WebGL canvas, since a clipped launcher hides the very buttons it is for.
			var scale = Mathf.Max(1f, Screen.height / 720f);
			if (InLauncher) {
				scale = Mathf.Min(scale, Screen.height / (PanelHeight + 24f), Screen.width / (PanelWidth + 24f));
			}
			GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
			var width = Screen.width / scale;
			var height = Screen.height / scale;

			if (InLauncher) {
				DrawLauncher(width, height);
			}
			else
			if (GUI.Button(new Rect(12, height - 44, 120, 32), "◀  Samples", backStyle)) {
				Open(launcherBuildIndex);
			}
		}

		private const float PanelWidth = 420f;
		private const float RowHeight = 46f;
		private const float RowGap = 8f;
		private const float IconSize = 72f;

		private float PanelHeight => 24f + (icon != null ? IconSize + 12f : 0f) + 64f + Mathf.Max(1, samples.Count) * (RowHeight + RowGap) + 16f;

		private void DrawLauncher(float width, float height) {
			var panelHeight = PanelHeight;
			var panel = new Rect((width - PanelWidth) / 2f, Mathf.Max(12f, (height - panelHeight) / 2f), PanelWidth, panelHeight);

			DrawRect(panel, Panel);
			DrawRect(new Rect(panel.x, panel.y, panel.width, 3f), Accent);

			var y = panel.y + 24f;
			if (icon != null) {
				GUI.DrawTexture(new Rect(panel.center.x - IconSize / 2f, y, IconSize, IconSize), icon, ScaleMode.ScaleToFit);
				y += IconSize + 12f;
			}
			GUI.Label(new Rect(panel.x, y, panel.width, 30f), "Narramancer Samples", titleStyle);
			GUI.Label(new Rect(panel.x, y + 30f, panel.width, 22f), "Save and load from inside any of them.", subtitleStyle);
			y += 64f;

			if (samples.Count == 0) {
				GUI.Label(new Rect(panel.x, y, panel.width, RowHeight), "No other scenes are in Build Settings.", subtitleStyle);
			}
			foreach (var (buildIndex, name) in samples) {
				var row = new Rect(panel.x + 20f, y, panel.width - 40f, RowHeight);
				var hovered = row.Contains(Event.current.mousePosition);
				DrawRect(row, Row);
				DrawRect(new Rect(row.x, row.y, 4f, row.height), hovered ? Accent : Slate * new Color(1f, 1f, 1f, 0.35f));

				SplitNumber(name, out var number, out var title);
				GUI.Label(new Rect(row.x + 16f, row.y, 28f, row.height), number, numberStyle);
				if (GUI.Button(new Rect(row.x + 44f, row.y, row.width - 44f, row.height), title, rowStyle)) {
					Open(buildIndex);
				}
				y += RowHeight + RowGap;
			}
		}

		/// <summary>"1. Text and Choices Example" → "1" and "Text and Choices Example".</summary>
		private static void SplitNumber(string name, out string number, out string title) {
			var dot = name.IndexOf(". ", System.StringComparison.Ordinal);
			if (dot > 0 && int.TryParse(name.Substring(0, dot), out _)) {
				number = name.Substring(0, dot);
				title = name.Substring(dot + 2);
			}
			else {
				number = "•";
				title = name;
			}
		}

		private void EnsureStyles() {
			if (titleStyle != null) {
				return;
			}
			titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
			titleStyle.normal.textColor = Color.white;
			subtitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, alignment = TextAnchor.MiddleCenter };
			subtitleStyle.normal.textColor = Slate;
			numberStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft };
			numberStyle.normal.textColor = Accent;

			// Transparent backgrounds: the rows are painted underneath, so hover is just a brighter label.
			rowStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, alignment = TextAnchor.MiddleLeft };
			rowStyle.normal.textColor = Slate;
			rowStyle.hover.textColor = Color.white;
			rowStyle.active.textColor = Accent;

			backStyle = new GUIStyle(GUI.skin.button) { fontSize = 13, fontStyle = FontStyle.Bold };
			backStyle.normal.background = Solid(new Color(Navy.r, Navy.g, Navy.b, 0.9f));
			backStyle.hover.background = Solid(Accent);
			backStyle.active.background = Solid(Accent * 0.85f);
			backStyle.normal.textColor = Slate;
			backStyle.hover.textColor = Color.white;
			backStyle.active.textColor = Color.white;
			backStyle.border = new RectOffset(0, 0, 0, 0);
		}

		private static readonly Dictionary<Color, Texture2D> solids = new Dictionary<Color, Texture2D>();

		private static Texture2D Solid(Color color) {
			if (!solids.TryGetValue(color, out var texture) || texture == null) {
				texture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
				texture.SetPixel(0, 0, color);
				texture.Apply();
				solids[color] = texture;
			}
			return texture;
		}

		private static void DrawRect(Rect rect, Color color) {
			GUI.DrawTexture(rect, Solid(color));
		}

		#endregion
	}
}
