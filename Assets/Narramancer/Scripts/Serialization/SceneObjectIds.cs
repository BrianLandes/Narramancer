using System;
using UnityEngine;

namespace Narramancer.Serialization {

	/// <summary>
	/// Names a <b>scene</b> object in a save, using the guid on its <see cref="Saveable"/>.
	/// <para>
	/// Scene objects had no durable identity until <see cref="Saveable"/> existed, which is why
	/// <c>Blackboard.gameObjects</c> and <c>.components</c> were marked <c>[NonSerialized]</c> from the first
	/// commit: a save simply could not refer to one. The cost was quiet — the Character Equipment sample stored
	/// its UI <c>Text</c> objects in a blackboard, those references were dropped on save, and after a load
	/// <c>SetTextNode</c> found null and did nothing, so the labels sat at their scene defaults with no error.
	/// </para>
	/// <para>
	/// A component id carries the type as well, because one GameObject can hold several. Two components of the
	/// same type on one object are not distinguishable this way and the first is returned — rare enough to
	/// accept, and it fails visibly rather than silently.
	/// </para>
	/// </summary>
	public static class SceneObjectIds {

		/// <summary>Marks an id as a scene object rather than an asset.</summary>
		public const string Prefix = "scene:";

		private const char ComponentSeparator = '/';

		public static bool IsSceneId(string id) => id != null && id.StartsWith(Prefix, StringComparison.Ordinal);

		/// <summary>
		/// True for a GameObject or Component living in a loaded scene. Such an object needs a
		/// <see cref="Saveable"/> to be nameable — which is different advice from an asset, so callers reporting a
		/// failure need to be able to tell them apart.
		/// </summary>
		public static bool IsSceneObject(UnityEngine.Object value) {
			var gameObject = value as GameObject;
			if (gameObject == null && value is Component component) {
				gameObject = component.gameObject;
			}
			return gameObject != null && gameObject.scene.IsValid();
		}

		/// <summary>
		/// The id for a scene GameObject or Component, or false when it has no <see cref="Saveable"/> — an
		/// object with no identity cannot be named, and the caller reports that.
		/// </summary>
		public static bool TryGetId(UnityEngine.Object value, out string id) {
			id = null;
			if (value == null) {
				return false;
			}

			var gameObject = value as GameObject;
			var component = value as Component;
			if (gameObject == null && component != null) {
				gameObject = component.gameObject;
			}
			if (gameObject == null || !gameObject.scene.IsValid()) {
				return false;
			}

			var saveable = gameObject.GetComponent<Saveable>();
			if (saveable == null) {
				return false;
			}

			var guid = saveable.EnsureGuid();
			if (guid.IsNullOrEmpty()) {
				return false;
			}

			id = component != null && !(value is GameObject)
				? $"{Prefix}{guid}{ComponentSeparator}{component.GetType().Name}"
				: Prefix + guid;
			return true;
		}

		/// <summary>The live object an id names, or false if this scene does not currently hold it.</summary>
		public static bool TryResolve(string id, Type type, out UnityEngine.Object value) {
			value = null;
			if (!IsSceneId(id)) {
				return false;
			}

			var body = id.Substring(Prefix.Length);
			var separator = body.IndexOf(ComponentSeparator);
			var guid = separator < 0 ? body : body.Substring(0, separator);
			var componentTypeName = separator < 0 ? null : body.Substring(separator + 1);

			if (!Saveable.TryFind(guid, out var saveable)) {
				return false;
			}

			if (componentTypeName == null) {
				value = saveable.gameObject;
				return type == null || type.IsInstanceOfType(value);
			}

			foreach (var component in saveable.GetComponents<Component>()) {
				if (component != null && component.GetType().Name == componentTypeName) {
					value = component;
					return type == null || type.IsInstanceOfType(value);
				}
			}
			return false;
		}
	}
}
