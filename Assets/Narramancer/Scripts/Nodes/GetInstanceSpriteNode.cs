using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("GameObject/Get Instance Sprite")]
	public class GetInstanceSpriteNode : AbstractInstanceInputNode {

		[SerializeField]
		[Input(ShowBackingValue.Unconnected, ConnectionType.Override)]
		private string key = "image";

		[SerializeField]
		[Output(ShowBackingValue.Never, ConnectionType.Override)]
		private Sprite sprite = default;

		public override object GetValue(INodeContext context, NodePort port) {
			if (Application.isPlaying) {
				switch (port.fieldName) {
					case nameof(sprite):
						var instance = GetInstance(context);
						if (instance != null) {
							var key = GetInputValue(context, nameof(this.key), this.key);
							var blackboard = instance.Blackboard;
							var blackboardValue = blackboard.Get<UnityEngine.Object>(key);
							if (blackboardValue != null) {
								if (typeof(Sprite).IsAssignableFrom(blackboardValue.GetType())) {
									return blackboardValue;
								}
								if (blackboardValue is Texture2D texture2D) {
									var sprite = Sprite.Create(texture2D, new Rect(0,0,texture2D.width, texture2D.height), Vector2.one * 0.5f);
									return sprite;
								}
							}
						}
						break;
						
				}

			}
			return base.GetValue(context, port);
		}
	}

}
