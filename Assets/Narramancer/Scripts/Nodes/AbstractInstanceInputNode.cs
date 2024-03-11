
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {

	public interface IAbstractInstanceInputNode {
		NounInstance GetInstance(INodeContext context);
	}

	public abstract class AbstractInstanceInputNode : Node, IAbstractInstanceInputNode {


		[SerializeField]
		[HideInInspector] // handled by AbstractInstanceInputNodeEditor
		[NodeEnum]
		protected InstanceAssignmentType nounType = InstanceAssignmentType.Instance;
		public static string NounTypeField => "nounType";

		[Input(backingValue = ShowBackingValue.Never, connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		[HideInInspector] // handled by AbstractInstanceInputNodeEditor
		protected NounInstance instance;
		public static string NounInstanceField => "instance";

		[Input(backingValue = ShowBackingValue.Unconnected, connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		[HideInInspector] // handled by AbstractInstanceInputNodeEditor
		protected NounScriptableObject scriptableObject;
		public static string NounScriptableObjectField => "scriptableObject";

		[Output]
		[SerializeField]
		[HideInInspector] // handled by AbstractInstanceInputNodeEditor
		protected NounInstance passThroughInstance;
		public static string PassThroughInstanceField => "passThroughInstance";


		public NounInstance GetInstance(INodeContext context) {
			switch (nounType) {
				case InstanceAssignmentType.Instance:
					return GetInputValue<NounInstance>(context, nameof(this.instance));
				default:
				case InstanceAssignmentType.ScriptableObject:
					var scriptableObject = GetInputValue(context, nameof(this.scriptableObject), this.scriptableObject);
					var instance = NarramancerSingleton.Instance.GetInstance(scriptableObject);
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
			return null;
		}
	}
}