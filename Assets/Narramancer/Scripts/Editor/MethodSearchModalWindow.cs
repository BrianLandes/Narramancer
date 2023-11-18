using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Narramancer {
	public class MethodSearchModalWindow : EditorWindow {

		Vector2 scrollPosition;
		string search;
		MethodInfo[] methods = null;
		MethodInfo[] filteredMethods = null;
		Action<MethodInfo> onSelect;
		bool autoFocused = false;

		const float windowHeight = 200;

		public void SearchTypes(Vector2 position, Type[] types, Action<MethodInfo> onSelect) {
			this.position = new Rect(position.x, position.y, 500, windowHeight);
			if (types != null) {
				this.methods = types.WithoutNulls().SelectMany(type => {
					// static classes
					if (type.IsAbstract && type.IsSealed) {
						return type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
					}
					return type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
						.Union(type.GetExtensionMethods());
				}).ToArray();
			}
			this.onSelect = onSelect;
			ShowPopup();
		}

		void OnGUI() {

			if (methods == null) {
				EditorGUILayout.LabelField("No valid methods found");
				return;
			}

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

			if (EditorGUI.EndChangeCheck() || filteredMethods == null) {

				var searchLower = search?.ToLower();
				filteredMethods = methods.Where(method => searchLower.IsNullOrEmpty() || method.Name.ToLower().Contains(searchLower) || method.ReflectedType.FullName.ToLower().Contains(searchLower)).ToArray();

				scrollPosition.y = 0;
			}

			scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

			var style = new GUIStyle(GUI.skin.button);
			style.alignment = TextAnchor.UpperLeft;

			var itemHeight = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

			var itemsAboveView = Mathf.Max(0, (int)(scrollPosition.y / itemHeight) - 1);

			GUILayout.Space(itemsAboveView * itemHeight);

			var itemsVisible = (int)(windowHeight / itemHeight) + 2;


			string MethodDescription(MethodInfo methodInfo) {
				var methodName = methodInfo.Name;
				var parameters = methodInfo.GetParameters();
				if (parameters == null || parameters.Length == 0) {
					return $"{methodName}()";
				}
				var parameterString = parameters.Select(parameter => parameter.ParameterType.Name).CommaSeparated();
				return $"{methodName}({parameterString})";
			}

			string MethodTooltip(MethodInfo methodInfo) {
				var className = methodInfo.ReflectedType.FullName;
				var methodName = methodInfo.Name;
				var parameters = methodInfo.GetParameters();
				if (parameters == null || parameters.Length == 0) {
					return $"{className}.{methodName}()";
				}
				var parameterString = parameters.Select(parameter => parameter.ParameterType.FullName).CommaSeparated();
				return $"{className}.{methodName}({parameterString})";
			}

			for (int i = 0; i < itemsVisible && itemsAboveView + i < filteredMethods.Count(); i++) {
				var method = filteredMethods[itemsAboveView + i];
				if (GUILayout.Button(new GUIContent(MethodDescription(method), MethodTooltip(method)), style)) {
					onSelect.Invoke(method);
					Close();
				}
			}

			var itemsBelowView = Mathf.Max(0, filteredMethods.Count() - itemsAboveView - itemsVisible);

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