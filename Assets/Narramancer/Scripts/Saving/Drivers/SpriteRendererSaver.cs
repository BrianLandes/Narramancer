using System;
using UnityEngine;

namespace Narramancer {

	public class SpriteRendererSaver : ComponentSaver<SpriteRenderer, SpriteRendererSaver.State> {

		[Serializable]
		public class State {
			public bool enabled;
			public Color color;
			public bool flipX;
			public bool flipY;
			public SavedSprite sprite;
		}

		public override string Key => "spriteRenderer";

		public override State Capture(SpriteRenderer spriteRenderer) {
			return new State {
				enabled = spriteRenderer.enabled,
				color = spriteRenderer.color,
				flipX = spriteRenderer.flipX,
				flipY = spriteRenderer.flipY,
				sprite = SavedSprite.From(spriteRenderer.sprite, spriteRenderer),
			};
		}

		public override void Apply(SpriteRenderer spriteRenderer, State state) {
			spriteRenderer.enabled = state.enabled;
			spriteRenderer.color = state.color;
			spriteRenderer.flipX = state.flipX;
			spriteRenderer.flipY = state.flipY;
			if (state.sprite != null) {
				spriteRenderer.sprite = state.sprite.ToSprite();
			}
		}
	}
}
