using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Narramancer {

	public class TypeSearchModalWindow : EditorWindow {

		Vector2 scrollPosition;
		string search;
		Type[] types = null;
		Type[] filteredTypes = null;
		Action<Type> onSelectType;
		bool autoFocused = false;

		const float windowHeight = 200;

		public void SearchTypes(Vector2 position, Type[] types, Action<Type> onSelectType) {
			this.position = new Rect(position.x, position.y, 300, windowHeight);
			this.types = types ?? AssemblyUtilities.GetAllPublicTypes().ToArray();
			this.onSelectType = onSelectType;
			ShowPopup();
		}

		void OnGUI() {

			EditorGUI.BeginChangeCheck();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.LabelField(EditorGUIUtility.IconContent("d_Search Icon"), GUILayout.Width(20));
			GUI.SetNextControlName(nameof(search));
			search = EditorGUILayout.TextField(search);
			if (!autoFocused) {
				EditorGUI.FocusTextInControl(nameof(search));
				autoFocused = true;
			}
			EditorGUILayout.EndHorizontal();

			if (EditorGUI.EndChangeCheck() || filteredTypes == null) {

				var searchLower = search?.ToLower();
				if (searchLower.IsNullOrEmpty()) {
					filteredTypes = types;
				}
				else {
					var searchTerms = searchLower.Split(' ');
					filteredTypes = types.Where(type => searchLower.IsNullOrEmpty() || searchTerms.All(term => type.FullName.ToLower().Contains(term))).ToArray();
				}
				

				scrollPosition.y = 0;
			}

			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

			var style = new GUIStyle(GUI.skin.button);
			style.alignment = TextAnchor.UpperLeft;

			var itemHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

			var itemsAboveView = Mathf.Max(0, (int)(scrollPosition.y / itemHeight) - 1);

			GUILayout.Space(itemsAboveView * itemHeight);

			var itemsVisible = (int)(windowHeight / itemHeight) + 2;

			for (int i = 0; i < itemsVisible && itemsAboveView + i < filteredTypes.Count(); i++) {
				var type = filteredTypes[itemsAboveView + i];
				if (GUILayout.Button(new GUIContent(type.Name, type.FullName), style)) {
					onSelectType.Invoke(type);
					Close();
				}
			}

			var itemsBelowView = Mathf.Max(0, filteredTypes.Count() - itemsAboveView - itemsVisible);

			GUILayout.Space(itemsBelowView * itemHeight);

			GUILayout.Space(20);

			EditorGUILayout.EndScrollView();
		}

		void OnInspectorUpdate() {
			Repaint();
		}

		private void OnLostFocus() {
			Close();
		}
	}

}