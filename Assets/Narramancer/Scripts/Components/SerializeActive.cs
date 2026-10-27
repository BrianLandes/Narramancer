namespace Narramancer {

	public class SerializeActive : SerializableMonoBehaviour {

		public override void Serialize(StoryInstance story) {
			base.Serialize(story);
			story.SaveTable.Set(Key("active"), gameObject.activeSelf);
		}

		public override void Deserialize(StoryInstance map) {
			base.Deserialize(map);
			if (!valuesOverwrittenByDeserialize) {
				return;
			}
			var active = map.SaveTable.GetAndRemove<bool>(Key("active"));
			gameObject.SetActive(active);
		}
	}
}