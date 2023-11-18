using UnityEngine;
using XNode;

namespace Narramancer {
	public class CommentNode : Node {

		[SerializeField, HideInInspector]
		private int width = 300;
		public static string WidthFieldName => nameof(width);

		[SerializeField, HideInInspector]
		private int height = 80;
		public static string HeightFieldName => nameof(height);

		[SerializeField]
		private Color color = new Color(0.4f, 0.8f, 0.4f, 0.2f);
		public static string ColorFieldName => nameof(color);

		[SerializeField]
		[TextArea(4, 30)]
		public string comment = "";

		public override object GetValue(object context, NodePort port) {
			return null;
		}

	}
}