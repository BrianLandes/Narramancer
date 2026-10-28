using System;
using UnityEngine;
using UnityEngine.UI;

namespace Narramancer {

	public class TextSaver : ComponentSaver<Text, TextSaver.State> {

		[Serializable]
		public class State {
			public bool enabled;
			public string text;
			public Color color;
		}

		public override string Key => "text";

		public override State Capture(Text text) {
			return new State {
				enabled = text.enabled,
				text = text.text,
				color = text.color,
			};
		}

		public override void Apply(Text text, State state) {
			text.enabled = state.enabled;
			text.text = state.text;
			text.color = state.color;
		}
	}
}
