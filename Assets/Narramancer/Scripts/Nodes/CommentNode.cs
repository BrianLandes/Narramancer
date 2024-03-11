using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	public class CommentNode : ResizableNode {

		[SerializeField]
		private Color color = new Color(0.4f, 0.8f, 0.4f, 0.2f);
		public static string ColorFieldName => nameof(color);

		[SerializeField]
		[TextArea(4, 30)]
		public string comment = "";

		public override object GetValue(INodeContext context, NodePort port) {
			return null;
		}

	}
}