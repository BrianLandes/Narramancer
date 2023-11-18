
using System;
namespace Narramancer {
	[Serializable]
	public class ToggleableValue<T> {

		public bool activated = false;

		public T value = default;

		public ToggleableValue() { }

		public ToggleableValue(bool activated) {
			this.activated = activated;
		}

		public ToggleableValue(T value) {
			this.activated = true;
			this.value = value;
		}

		public ToggleableValue(bool activated, T value) {
			this.activated = activated;
			this.value = value;
		}

		public static implicit operator bool(ToggleableValue<T> togglableValue) => togglableValue.activated;

		public static implicit operator T(ToggleableValue<T> togglableValue) => togglableValue.value;

		public static implicit operator ToggleableValue<T>(T value) {
			var newValue = Activator.CreateInstance<ToggleableValue<T>>();
			newValue.activated = true;
			newValue.value = value;
			return newValue;
		}

		public static implicit operator ToggleableValue<T>(bool someBool) {
			var newValue = Activator.CreateInstance<ToggleableValue<T>>();
			newValue.activated = someBool;
			return newValue;
		}
	}
}