
using System;
namespace Narramancer {
	[Serializable]
	public class ToggleableString : ToggleableValue<string> {
		public ToggleableString() { }

		public ToggleableString(bool activated) : base(activated) { }

		public ToggleableString(string value) : base(value) { }
	}
}