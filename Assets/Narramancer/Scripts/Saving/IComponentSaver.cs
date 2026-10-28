using System;
using UnityEngine;

namespace Narramancer {

	/// <summary>
	/// Saves one kind of component — a "driver". <see cref="Saveable"/> finds the drivers that apply to the
	/// components on its object, and each one copies its component's state to and from a plain state object.
	/// <para>
	/// Drivers hold no state of their own: one instance serves every object in the game. In-flight behaviour —
	/// a tween under way, the promise a verb is waiting on — lives in a behaviour component with
	/// <see cref="SaveAttribute"/> fields instead (<see cref="Tweener"/> is the example).
	/// </para>
	/// <para>
	/// To support a new component type, derive from <see cref="ComponentSaver{TComponent, TState}"/> and call
	/// <see cref="ComponentSaverRegistry.Register"/> before the first save, e.g. from a
	/// <c>[RuntimeInitializeOnLoadMethod]</c>.
	/// </para>
	/// </summary>
	public interface IComponentSaver {

		/// <summary>The component this saves. Matched exactly first, then by base type, so a more specific driver wins.</summary>
		Type ComponentType { get; }

		/// <summary>Names this driver's entry in a save. Never change it once shipped — saves written under the old key stop loading.</summary>
		string Key { get; }

		/// <summary>What <see cref="Capture"/> returns. A plain <c>[Serializable]</c> class with public fields.</summary>
		Type StateType { get; }

		object Capture(Component component);

		void Apply(Component component, object state);
	}

	/// <summary>The typed way to write an <see cref="IComponentSaver"/>.</summary>
	public abstract class ComponentSaver<TComponent, TState> : IComponentSaver where TComponent : Component {

		public Type ComponentType => typeof(TComponent);

		public Type StateType => typeof(TState);

		public abstract string Key { get; }

		public abstract TState Capture(TComponent component);

		public abstract void Apply(TComponent component, TState state);

		object IComponentSaver.Capture(Component component) => Capture((TComponent)component);

		void IComponentSaver.Apply(Component component, object state) => Apply((TComponent)component, (TState)state);
	}
}
