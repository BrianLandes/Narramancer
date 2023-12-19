using System;
using UnityEngine;
using UnityEngine.UI;

namespace Narramancer {

	[RequireComponent(typeof(Image))]
	public class SerializeImage : SerializableMonoBehaviour {

		public override void Serialize(StoryInstance story) {
			base.Serialize(story);
			var image = GetComponent<Image>();
			if (image != null) {
				var hasSprite = image.sprite != null;
				story.SaveTable.Set(Key("hasSprite"), hasSprite);
				if (hasSprite) {
					story.SaveTable.Set(Key("texture"), image.sprite.texture);
					story.SaveTable.Set(Key("rect"), image.sprite.rect);
					story.SaveTable.Set(Key("pivot"), image.sprite.pivot);
				}
				story.SaveTable.Set(Key("color"), image.color);
			}

		}

		public override void Deserialize(StoryInstance map) {
			base.Deserialize(map);
			var image = GetComponent<Image>();
			if (image != null) {
				var hasSprite = map.SaveTable.GetAndRemove<bool>(Key("hasSprite"));
				if (hasSprite) {
					var texture = map.SaveTable.GetAndRemove<Texture2D>(Key("texture"));
					var rect = map.SaveTable.GetAndRemove<Rect>(Key("rect"));
					var pivot = map.SaveTable.GetAndRemove<Vector2>(Key("pivot"));
					pivot /= rect.size;
					image.sprite = Sprite.Create(texture, rect, pivot);
				}


				image.color = map.SaveTable.Get<Color>(Key("color"));
			}

		}
	}
}