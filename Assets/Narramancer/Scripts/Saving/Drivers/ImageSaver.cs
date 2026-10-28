using System;
using UnityEngine;
using UnityEngine.UI;

namespace Narramancer {

	public class ImageSaver : ComponentSaver<Image, ImageSaver.State> {

		[Serializable]
		public class State {
			public bool enabled;
			public Color color;
			public SavedSprite sprite;
		}

		public override string Key => "image";

		public override State Capture(Image image) {
			return new State {
				enabled = image.enabled,
				color = image.color,
				sprite = SavedSprite.From(image.sprite, image),
			};
		}

		public override void Apply(Image image, State state) {
			image.enabled = state.enabled;
			image.color = state.color;
			if (state.sprite != null) {
				image.sprite = state.sprite.ToSprite();
			}
		}
	}
}
