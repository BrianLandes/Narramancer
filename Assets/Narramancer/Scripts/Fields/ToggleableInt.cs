
using System;
namespace Narramancer {
	[Serializable]
	public class ToggleableInt : ToggleableValue<int> {
		public ToggleableInt() { }

		public ToggleableInt(bool activated) : base(activated) { }

		public ToggleableInt(int value) : base(value) { }
	}
}