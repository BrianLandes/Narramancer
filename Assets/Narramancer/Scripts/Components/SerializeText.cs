using System;
using UnityEngine;
using UnityEngine.UI;

namespace Narramancer {

	[RequireComponent(typeof(Text))]
	public class SerializeText : SerializableMonoBehaviour {

		public override void Serialize(StoryInstance story) {
			base.Serialize(story);
			var component = GetComponent<Text>();
			story.SaveTable.Set(Key("text"), component.text);
		}

		public override void Deserialize(StoryInstance map) {
			base.Deserialize(map);
			var component = GetComponent<Text>();
			component.text = map.SaveTable.GetAndRemove<string>(Key("text"));
		}
	}
}