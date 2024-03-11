
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {

	[CreateNodeMenu("GameObject/Find GameObject with Tag")]
	public class FindGameObjectWithTagNode : Node {

		[Input(connectionType =ConnectionType.Override, typeConstraint =TypeConstraint.Inherited)]
		[SerializeField]
		private string tag = "";

		[Output]
		[SerializeField]
		private GameObject gameObject = default;

		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying && port.fieldName.Equals(nameof(gameObject))) {
				var inputTag = GetInputValue(context, nameof(tag), tag);
				var foundGameObject = GameObject.FindGameObjectWithTag(inputTag);
				return foundGameObject;
			}
			return null;
		}

	}
}
