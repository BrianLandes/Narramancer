
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {

	[CreateNodeMenu("GameObject/Find GameObject with Name")]
	public class FindGameObjectWithNameNode : Node {

		[Input(connectionType =ConnectionType.Override, typeConstraint =TypeConstraint.Inherited)]
		[SerializeField, HideLabel]
		private string targetName = "";

		[Output]
		[SerializeField, HideLabel, SameLine]
		private GameObject gameObject = default;

		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying && port.fieldName.Equals(nameof(gameObject))) {
				var inputName = GetInputValue(context, nameof(targetName), targetName);
				var foundGameObject = GameObject.Find(inputName);
				return foundGameObject;
			}
			return null;
		}

	}
}
