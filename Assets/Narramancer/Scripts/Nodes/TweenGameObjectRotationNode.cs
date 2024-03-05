using UnityEngine;
using XNode;

namespace Narramancer {

	[CreateNodeMenu("GameObject/Tween GameObject Rotation")]
	public class TweenGameObjectRotationNode : ChainedRunnableNode {

		[Input(connectionType = ConnectionType.Multiple, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		private GameObject gameObject = default;

		[Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		private Quaternion targetRotation = default;

		public enum MoveType {
			//Duration,
			Speed,
			Immediate
		}
		[SerializeField]
		[NodeEnum]
		private MoveType moveType = MoveType.Speed;

		//[Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		//[SerializeField]
		//[Min(0)]
		//private float duration = 1f;

		[Input(connectionType = ConnectionType.Override, typeConstraint = TypeConstraint.Inherited)]
		[SerializeField]
		[Min(0.00001f)]
		private float moveSpeed = 1f;

		[SerializeField]
		private bool wait = true;

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			var inputGameObjects = GetInputValues(runner.Blackboard, nameof(gameObject), gameObject);

			var targetRotation = GetInputValue(runner.Blackboard, nameof(this.targetRotation), this.targetRotation);

			foreach( var inputGameObject in inputGameObjects) {
				float speed = 0f;
				switch (moveType) {
					//case MoveType.Duration:
					//	var duration = GetInputValue(runner.Blackboard, nameof(this.duration), this.duration);
					//	var distance = (inputGameObject.transform.position - inputTargetPosition).magnitude;
					//	if (duration > 0) {
					//		speed = distance / duration;
					//	}
					//	else {
					//		speed = 0f;
					//	}
					//	break;
					case MoveType.Speed:
						speed = GetInputValue(runner.Blackboard, nameof(this.moveSpeed), this.moveSpeed);
						break;
					case MoveType.Immediate:
						speed = 0f;
						break;
				}

				if (speed > 0) {
					Promise promise = null;

					var serializeTransform = inputGameObject.GetComponent<SerializeTransform>();
					if (serializeTransform != null) {
						promise = serializeTransform.TweenTo(targetRotation, speed);
					}
					else {
						//var serializeRectTransform = inputGameObject.GetComponent<SerializeRectTransform>();
						//if (serializeRectTransform != null) {
						//	promise = serializeRectTransform.TweenTo(targetRotation, speed);
						//}
						//else {
							Debug.LogWarning("Target GameObject has no way to translate it: " + inputGameObject.name);
						//	// TODO: animate the object still
						//}
					}

					if (promise != null && wait) {
						runner.Suspend();
						promise.WhenDone(() => {
							runner.Resume();
						});
					}
				}
				else {
					inputGameObject.transform.rotation = targetRotation;
				}
			}
		}
	}
}
