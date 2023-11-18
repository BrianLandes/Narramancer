namespace Narramancer {
	public interface ISerializableMonoBehaviour {

		void Serialize(StoryInstance story);

		void Deserialize(StoryInstance story);
	}
}