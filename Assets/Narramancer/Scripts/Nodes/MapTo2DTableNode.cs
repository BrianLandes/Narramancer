using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {

	[NodeWidth(600)]
	[CreateNodeMenu("Math/Map Float To 2D Table")]
	public class MapTo2DTableNode : ResizableNode {

		[Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		protected float x = 0;

		[Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		protected float y = 0;

		[Output(connectionType = ConnectionType.Multiple, typeConstraint = TypeConstraint.Inherited, backingValue = ShowBackingValue.Always)]
		[SerializeField]
		protected float result;

		[SerializeField]
		protected VectorFieldTable vectorFieldTable;

		public override object GetValue(INodeContext context, NodePort port) {
			if (port.fieldName.Equals(nameof(result))) {

				float xValue = GetInputValue(context, nameof(x), x);
				float yValue = GetInputValue(context, nameof(y), y);

				result = vectorFieldTable.Calculate(xValue, yValue);

				return result;
			}
			return null;
		}
	}
}
