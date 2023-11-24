using System;
using UnityEngine;

namespace Narramancer {
	[RequireComponent(typeof(SpriteRenderer))]
	public class SerializeSpriteRenderer : SerializableMonoBehaviour {


		public override void Serialize(StoryInstance story) {
			base.Serialize(story);
			var spriteRenderer = GetComponent<SpriteRenderer>();
			story.SaveTable.Set(Key("texture"), spriteRenderer.sprite.texture);
			story.SaveTable.Set(Key("rect"), spriteRenderer.sprite.rect);
			var pivot = spriteRenderer.sprite.pivot;
			story.SaveTable.Set(Key("pivot"), pivot);
		}

		public override void Deserialize(StoryInstance map) {
			base.Deserialize(map);
			var texture = map.SaveTable.GetAndRemove<Texture2D>(Key("texture"));
			var rect = map.SaveTable.GetAndRemove<Rect>(Key("rect"));
			var pivot = map.SaveTable.GetAndRemove<Vector2>(Key("pivot"));
			pivot /= rect.size;
			var sprite = Sprite.Create(texture, rect, pivot);
			var spriteRenderer = GetComponent<SpriteRenderer>();
			spriteRenderer.sprite = sprite;
		}
	}
}