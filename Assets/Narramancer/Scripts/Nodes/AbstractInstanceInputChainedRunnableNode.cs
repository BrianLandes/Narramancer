using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {

	public abstract class AbstractInstanceInputChainedRunnableNode : ChainedRunnableNode, IAbstractInstanceInputNode {

		[SerializeField]
		[HideInInspector] // handled by AbstractInstanceInputNodeEditor
		[NodeEnum]
		protected InstanceAssignmentType nounType = InstanceAssignmentType.Instance;

		[Input(backingValue = ShowBackingValue.Never, connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		[HideInInspector]
		protected NounInstance instance;

		[Input(backingValue = ShowBackingValue.Unconnected, connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		[HideInInspector]
		protected NounScriptableObject scriptableObject;

		[Output]
		[SerializeField]
		[HideInInspector]
		protected NounInstance passThroughInstance;


		public NounInstance GetInstance(INodeContext context) {
			switch (nounType) {
				case InstanceAssignmentType.Instance:
					return GetInputValue<NounInstance>(context, nameof(this.instance));
				default:
				case InstanceAssignmentType.ScriptableObject:
					var predefinedNoun = GetInputValue(context, nameof(this.scriptableObject), this.scriptableObject);
					var instance = NarramancerSingleton.Instance.GetInstance(predefinedNoun);
					return instance;
			}
		}

		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying) {
				switch (port.fieldName) {
					case nameof(passThroughInstance):
						return GetInstance(context);
				}

			}
			return base.GetValue(context, port);
		}
	}
}