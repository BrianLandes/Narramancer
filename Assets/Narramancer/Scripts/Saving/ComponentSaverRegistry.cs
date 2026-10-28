using System;
using System.Collections.Generic;
using System.Linq;

namespace Narramancer {

	/// <summary>
	/// Which <see cref="IComponentSaver"/> saves which component type.
	/// <para>
	/// The built-in drivers are listed here by hand rather than discovered by reflection. A type reached only by
	/// reflection is exactly what IL2CPP's managed stripping removes, and it would fail only in a shipped build —
	/// see <c>docs/RELEASE_RUNBOOK.md</c>. A third-party driver is registered by its own code, which references it
	/// and so keeps it alive.
	/// </para>
	/// </summary>
	public static class ComponentSaverRegistry {

		private static readonly Dictionary<Type, IComponentSaver> byComponentType = new Dictionary<Type, IComponentSaver>();

		static ComponentSaverRegistry() {
			Register(new TransformSaver());
			Register(new RectTransformSaver());
			Register(new AudioSourceSaver());
			Register(new AnimationSaver());
			Register(new ImageSaver());
			Register(new SpriteRendererSaver());
			Register(new TextSaver());
#if NARRAMANCER_AI_MODULE
			Register(new NavMeshAgentSaver());
#endif
		}

		/// <summary>Adds a driver, replacing any already registered for the same component type.</summary>
		public static void Register(IComponentSaver saver) {
			if (saver == null) {
				throw new ArgumentNullException(nameof(saver));
			}
			var clash = byComponentType.Values.FirstOrDefault(other => other.Key == saver.Key && other.ComponentType != saver.ComponentType);
			if (clash != null) {
				throw new ArgumentException($"A driver for {clash.ComponentType.Name} already uses the key '{saver.Key}'; two drivers sharing one would overwrite each other's saves.");
			}
			byComponentType[saver.ComponentType] = saver;
		}

		/// <summary>
		/// The driver for <paramref name="componentType"/>: an exact match, else the nearest base type's. So a
		/// <c>RectTransform</c> gets the RectTransform driver, and a game's own <c>Image</c> subclass still gets
		/// the Image one.
		/// </summary>
		public static bool TryFind(Type componentType, out IComponentSaver saver) {
			for (var type = componentType; type != null; type = type.BaseType) {
				if (byComponentType.TryGetValue(type, out saver)) {
					return true;
				}
			}
			saver = null;
			return false;
		}

		public static bool TryFindByKey(string key, out IComponentSaver saver) {
			saver = byComponentType.Values.FirstOrDefault(candidate => candidate.Key == key);
			return saver != null;
		}

		public static IEnumerable<IComponentSaver> All => byComponentType.Values;
	}
}
