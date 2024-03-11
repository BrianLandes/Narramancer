
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {

	public class MakeColorListNode : Node {

		[SerializeField]
		private List<Color> colors = new List<Color>();

		[SerializeField]
		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
		private List<Color> outputColors = new List<Color>();

		public override object GetValue(INodeContext context, NodePort port) {
			if (port.fieldName.Equals(nameof(outputColors))) {

				return colors;
			}
			return null;
		}

	}
}