
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Narramancer {

	public abstract class AbstractSearchModalWindow<T> : EditorWindow {

		Vector2 scrollPosition;
		string search;
		T[] allValues;
		T[] filteredValues;
		Action<T> onSelect;
		bool autoFocused = false;

		protected abstract string GetName(T element);
		protected abstract string GetTooltip(T element);

		protected abstract bool ContainsAnySearchTerms(T element, string[] searchTerms);

		public virtual void ShowForValues(Vector2 position, IEnumerable<T> possibleValues, Action<T> onSelect) {
			this.position = new Rect(position.x, position.y, GetWindowWidth(), GetWindowHeight());
			allValues = possibleValues.ToArray();
			this.onSelect = onSelect;
			ShowPopup();
		}

		protected virtual float GetWindowWidth() {
			return 500;
		}

		protected virtual float GetWindowHeight() {
			return 200;
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

			if (EditorGUI.EndChangeCheck() || filteredValues == null) {

				if (search.IsNullOrEmpty()) {
					filteredValues = allValues;
				}
				else {
					var searchLower = search.ToLower();
					var searchTerms = searchLower.Split(' ');
					filteredValues = allValues.Where(type => ContainsAnySearchTerms(type, searchTerms)).ToArray();

					scrollPosition.y = 0;
				}
			}

			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

			var style = new GUIStyle(GUI.skin.button);
			style.alignment = TextAnchor.UpperLeft;

			var itemHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

			var itemsAboveView = Mathf.Max(0, (int)(scrollPosition.y / itemHeight) - 1);

			GUILayout.Space(itemsAboveView * itemHeight);

			var itemsVisible = (int)(position.height / itemHeight) + 2;

			for (int i = 0; i < itemsVisible && itemsAboveView + i < filteredValues.Count(); i++) {
				var element = filteredValues[itemsAboveView + i];
				var name = GetName(element);
				var tooltip = GetTooltip(element);
				if (GUILayout.Button(new GUIContent(name, tooltip), style)) {
					onSelect.Invoke(element);
					Close();
				}
			}

			var itemsBelowView = Mathf.Max(0, filteredValues.Count() - itemsAboveView - itemsVisible);

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