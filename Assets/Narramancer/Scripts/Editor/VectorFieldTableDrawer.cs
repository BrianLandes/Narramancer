
using UnityEditor;
using UnityEngine;

namespace Narramancer {

	[CustomPropertyDrawer(typeof(VectorFieldTable))]
	public class VectorFieldTableDrawer : PropertyDrawer {

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {

			label = EditorGUI.BeginProperty(position, label, property);

			var labelPosition = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

			property.isExpanded = EditorGUI.Foldout(labelPosition, property.isExpanded, label);
			if (property.isExpanded) {

				var indent = EditorGUI.indentLevel;
				EditorGUI.indentLevel = 0;

				position = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, position.height - EditorGUIUtility.singleLineHeight);

				var headerLabel = new GUIStyle(EditorStyles.label);
				headerLabel.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

				EditorGUI.DrawRect(position, new Color(0.2f, 0.2f, 0.2f));

				var headerBackgroundColor = new Color(0.3f, 0.3f, 0.3f);

				var rowLabelsWidth = 45;
				var tableWidth = position.width - rowLabelsWidth;
				int columns = Mathf.FloorToInt(tableWidth / rowLabelsWidth);
				float columnWidth = tableWidth / columns;

				var fieldHeight = EditorGUIUtility.singleLineHeight;

				var headerPosition = new Rect(position.x + columnWidth, position.y, tableWidth - columnWidth, fieldHeight);
				EditorGUI.DrawRect(headerPosition, headerBackgroundColor);

				// Draw Column Headers -> X Values from Min to Max

				{
					var fieldPosition = new Rect(position.x + columnWidth, position.y, columnWidth, fieldHeight);
					var serializedProperty = property.FindPropertyRelative(nameof(VectorFieldTable.minX));
					EditorGUI.PropertyField(fieldPosition, serializedProperty, GUIContent.none);
				}

				{
					var fieldPosition = new Rect(position.x + position.width - columnWidth, position.y, columnWidth, fieldHeight);
					var serializedProperty = property.FindPropertyRelative(nameof(VectorFieldTable.maxX));
					EditorGUI.PropertyField(fieldPosition, serializedProperty, GUIContent.none);
				}

				float minX = property.FindPropertyRelative(nameof(VectorFieldTable.minX)).floatValue;
				float maxX = property.FindPropertyRelative(nameof(VectorFieldTable.maxX)).floatValue;

				for (int x = 1; x < columns - 1; x++) {

					float percentage = (float)x / (float)columns;
					float value = minX + (maxX - minX) * percentage;

					var fieldPosition = new Rect(position.x + columnWidth + columnWidth * x, position.y, columnWidth, fieldHeight);
					EditorGUI.LabelField(fieldPosition, string.Format("{0:0.##}", value), headerLabel);
				}

				// Draw Row Headers -> Y Values from Min to Max

				var tableHeight = position.height - fieldHeight;
				int rows = Mathf.FloorToInt(tableHeight / (EditorGUIUtility.singleLineHeight + 4));
				float rowHeight = tableHeight / rows;

				headerPosition = new Rect(position.x + 2, position.y + rowHeight + 4, columnWidth - 4, tableHeight - rowHeight - 4);
				EditorGUI.DrawRect(headerPosition, headerBackgroundColor);

				{
					var fieldPosition = new Rect(position.x, position.y + rowHeight, columnWidth, fieldHeight);
					var serializedProperty = property.FindPropertyRelative(nameof(VectorFieldTable.minY));
					EditorGUI.PropertyField(fieldPosition, serializedProperty, GUIContent.none);
				}

				{
					var fieldPosition = new Rect(position.x, position.y + position.height - fieldHeight, columnWidth, fieldHeight);
					var serializedProperty = property.FindPropertyRelative(nameof(VectorFieldTable.maxY));
					EditorGUI.PropertyField(fieldPosition, serializedProperty, GUIContent.none);
				}


				float minY = property.FindPropertyRelative(nameof(VectorFieldTable.minY)).floatValue;
				float maxY = property.FindPropertyRelative(nameof(VectorFieldTable.maxY)).floatValue;

				for (int y = 1; y < rows - 1; y++) {

					float percentage = (float)y / (float)rows;
					float value = minY + (maxY - minY) * percentage;

					var fieldPosition = new Rect(position.x + 4, position.y + rowHeight + rowHeight * y, columnWidth - 4, fieldHeight);
					EditorGUI.LabelField(fieldPosition, string.Format("{0:0.##}", value), headerLabel);
				}

				// Draw the Float Fields for Output Values

				{
					var fieldPosition = new Rect(position.x + columnWidth, position.y + rowHeight, columnWidth, fieldHeight);
					var serializedProperty = property.FindPropertyRelative(nameof(VectorFieldTable.resultForMinXMinY));
					EditorGUI.PropertyField(fieldPosition, serializedProperty, GUIContent.none);
				}

				{
					var fieldPosition = new Rect(position.x + position.width - columnWidth, position.y + rowHeight, columnWidth, fieldHeight);
					var serializedProperty = property.FindPropertyRelative(nameof(VectorFieldTable.resultForMaxXMinY));
					EditorGUI.PropertyField(fieldPosition, serializedProperty, GUIContent.none);
				}

				{
					var fieldPosition = new Rect(position.x + columnWidth, position.y + position.height - fieldHeight, columnWidth, fieldHeight);
					var serializedProperty = property.FindPropertyRelative(nameof(VectorFieldTable.resultForMinXMaxY));
					EditorGUI.PropertyField(fieldPosition, serializedProperty, GUIContent.none);
				}

				{
					var fieldPosition = new Rect(position.x + position.width - columnWidth, position.y + position.height - fieldHeight, columnWidth, fieldHeight);
					var serializedProperty = property.FindPropertyRelative(nameof(VectorFieldTable.resultForMaxXMaxY));
					EditorGUI.PropertyField(fieldPosition, serializedProperty, GUIContent.none);
				}

				float resultForMinXMinY = property.FindPropertyRelative(nameof(VectorFieldTable.resultForMinXMinY)).floatValue;
				float resultForMaxXMinY = property.FindPropertyRelative(nameof(VectorFieldTable.resultForMaxXMinY)).floatValue;
				float resultForMinXMaxY = property.FindPropertyRelative(nameof(VectorFieldTable.resultForMinXMaxY)).floatValue;
				float resultForMaxXMaxY = property.FindPropertyRelative(nameof(VectorFieldTable.resultForMaxXMaxY)).floatValue;

				float maxResult = Mathf.Max(resultForMinXMinY, resultForMaxXMinY, resultForMinXMaxY, resultForMaxXMaxY);
				float minResult = Mathf.Min(resultForMinXMinY, resultForMaxXMinY, resultForMinXMaxY, resultForMaxXMaxY);

				float CalculateResult(float x, float y) {
					return VectorFieldTableUtilities.CalculateValue(x, y, minX, maxX, minY, maxY, resultForMinXMinY, resultForMaxXMinY, resultForMinXMaxY, resultForMaxXMaxY);
				}


				Color GetCellColor(float value) {
					value = VectorFieldTableUtilities.Normalize(value, minResult, maxResult) * 2f - 1f;
					var red = -1f * Mathf.Min(0, value);
					var green = Mathf.Max(0, value);
					return new Color(1f - green, 1f - red, 1f - green - red);
				}

				// Draw the first row of values

				for (int x = 1; x < columns - 1; x++) {

					float percentage = (float)x / (float)columns;
					float value = minX + (maxX - minX) * percentage;
					float result = CalculateResult(value, minY);

					var valueLabel = new GUIStyle(EditorStyles.label);
					valueLabel.normal.textColor = GetCellColor(result);

					var fieldPosition = new Rect(position.x + columnWidth + columnWidth * x, position.y + rowHeight, columnWidth, fieldHeight);
					EditorGUI.LabelField(fieldPosition, string.Format("{0:0.#}", result), valueLabel);
				}

				// Draw all the rest of the values EXCEPT the last one

				for (int x = 0; x < columns; x++) {
					float percentageX = (float)x / (float)columns;
					float valueX = minX + (maxX - minX) * percentageX;

					for (int y = 1; y < rows - 1; y++) {

						float percentageY = (float)y / (float)rows;
						float valueY = minY + (maxY - minY) * percentageY;
						float result = CalculateResult(valueX, valueY);

						var valueLabel = new GUIStyle(EditorStyles.label);
						valueLabel.normal.textColor = GetCellColor(result);

						var fieldPosition = new Rect(position.x + columnWidth + columnWidth * x, position.y + rowHeight + rowHeight * y, columnWidth, fieldHeight);
						EditorGUI.LabelField(fieldPosition, string.Format("{0:0.#}", result), valueLabel);
					}
				}


				// Draw the last row of values

				for (int x = 1; x < columns - 1; x++) {

					float percentage = (float)x / (float)columns;
					float value = minX + (maxX - minX) * percentage;
					float result = CalculateResult(value, maxY);

					var valueLabel = new GUIStyle(EditorStyles.label);
					valueLabel.normal.textColor = GetCellColor(result);

					var fieldPosition = new Rect(position.x + columnWidth + columnWidth * x, position.y + position.height - fieldHeight, columnWidth, fieldHeight);
					EditorGUI.LabelField(fieldPosition, string.Format("{0:0.#}", result), valueLabel);
				}

				// Draw grid

				{
					var spacerPosition = new Rect(position.x + columnWidth - 4, position.y, 2, position.height);
					EditorGUI.DrawRect(spacerPosition, new Color(0.1f, 0.1f, 0.1f));

					spacerPosition = new Rect(position.x + columnWidth + columnWidth - 4, position.y, 2, position.height);
					EditorGUI.DrawRect(spacerPosition, new Color(0.1f, 0.1f, 0.1f));
				}

				for (int x = 1; x < columns - 1; x++) {
					var spacerPosition = new Rect(position.x + columnWidth + columnWidth * x + columnWidth - 4, position.y, 2, position.height);
					EditorGUI.DrawRect(spacerPosition, new Color(0.1f, 0.1f, 0.1f));
				}

				{
					var spacerPosition = new Rect(position.x, position.y + rowHeight - 4, position.width, 2);
					EditorGUI.DrawRect(spacerPosition, new Color(0.1f, 0.1f, 0.1f));

					spacerPosition = new Rect(position.x, position.y + rowHeight + rowHeight - 4, position.width, 2);
					EditorGUI.DrawRect(spacerPosition, new Color(0.1f, 0.1f, 0.1f));
				}

				for (int y = 1; y < rows - 1; y++) {
					var spacerPosition = new Rect(position.x, position.y + rowHeight + rowHeight * y + rowHeight - 4, position.width, 2);
					EditorGUI.DrawRect(spacerPosition, new Color(0.1f, 0.1f, 0.1f));
				}


				EditorGUI.indentLevel = indent;

			}

			EditorGUI.EndProperty();
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
			if (property.isExpanded) {
				return EditorGUIUtility.singleLineHeight * 20;
			}
			return EditorGUIUtility.singleLineHeight;
		}
	}
}