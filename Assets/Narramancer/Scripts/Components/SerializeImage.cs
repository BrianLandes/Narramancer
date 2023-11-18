using System;
using UnityEngine;
using UnityEngine.UI;

namespace Narramancer {

	[RequireComponent(typeof(Image))]
	public class SerializeImage : SerializableMonoBehaviour {

		public override void Serialize(StoryInstance story) {
			base.Serialize(story);
			var image = GetComponent<Image>();
			story.Blackboard.Set(Key("texture"), image.sprite.texture);
			story.Blackboard.Set(Key("rect"), image.sprite.rect);
			story.Blackboard.Set(Key("pivot"), image.sprite.pivot);
		}

		public override void Deserialize(StoryInstance map) {
			base.Deserialize(map);
			var image = GetComponent<Image>();
			var texture = map.Blackboard.Get<Texture2D>(Key("texture"));
			var rect = map.Blackboard.Get<Rect>(Key("rect"));
			var pivot = map.Blackboard.Get<Vector2>(Key("pivot"));
			image.sprite = Sprite.Create(texture, rect, pivot);
		}
	}
}