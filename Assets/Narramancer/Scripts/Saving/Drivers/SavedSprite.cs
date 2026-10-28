using System;
using UnityEngine;

namespace Narramancer {

	/// <summary>
	/// A sprite as a save holds it: by reference when it can be named, rebuilt from its texture when it cannot.
	/// <para>
	/// The old savers always rebuilt it with <c>Sprite.Create</c>, which made a <em>new</em> sprite on every
	/// load — losing its border, pixels-per-unit and atlas packing, and breaking anything comparing sprites by
	/// reference. A sprite in the asset manifest or a <c>Resources</c> folder is now saved as itself; only one
	/// built at runtime (a save-slot thumbnail, say) falls back to its texture.
	/// </para>
	/// </summary>
	[Serializable]
	public class SavedSprite {
		public Sprite sprite;

		public Texture2D texture;
		public Rect rect;
		public Vector2 pivot;
		public Vector4 border;
		public float pixelsPerUnit;

		public static SavedSprite From(Sprite sprite, UnityEngine.Object context) {
			if (sprite == null) {
				return new SavedSprite();
			}
			if (Serialization.PersistentId.Of(sprite) != null) {
				return new SavedSprite { sprite = sprite };
			}
			if (Serialization.PersistentId.Of(sprite.texture) == null) {
				// Neither can be named, and writing the reference anyway would fail the whole save. Leave this one
				// sprite at its scene state instead, and say so.
				Debug.LogWarning(
					$"The sprite '{sprite.name}' on '{context.name}' cannot be saved: neither it nor its texture is in a Resources folder or the asset manifest. " +
					"It will load as the scene has it.", context);
				return null;
			}
			return new SavedSprite {
				texture = sprite.texture,
				rect = sprite.rect,
				pivot = sprite.pivot,
				border = sprite.border,
				pixelsPerUnit = sprite.pixelsPerUnit,
			};
		}

		public Sprite ToSprite() {
			if (sprite != null || texture == null) {
				return sprite;
			}
			// Sprite.pivot is in pixels; Sprite.Create wants it normalized.
			var normalizedPivot = new Vector2(pivot.x / rect.width, pivot.y / rect.height);
			return Sprite.Create(texture, rect, normalizedPivot, pixelsPerUnit, 0, SpriteMeshType.Tight, border);
		}
	}
}
