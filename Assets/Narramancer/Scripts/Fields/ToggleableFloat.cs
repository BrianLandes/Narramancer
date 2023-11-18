
using System;
namespace Narramancer {
	[Serializable]
	public class ToggleableFloat : ToggleableValue<float> {

		public ToggleableFloat() { }

		public ToggleableFloat(bool activated) : base(activated) { }

		public ToggleableFloat(float value) : base(value) { }

		public ToggleableFloat(bool activated, float value) : base(activated, value) { }

	}
}