using System.Linq;

namespace Narramancer {

	public class SerializeNounInstanceReference : SerializableMonoBehaviour {

		[SerializeMonoBehaviourField]
		private NounInstance instance;

		public override void Serialize(StoryInstance story) {

			if (instance == null) {
				instance = NarramancerSingleton.Instance.GetInstances().FirstOrDefault(instance => instance.GameObject == gameObject);
			}

			base.Serialize(story);
		}

		public override void Deserialize(StoryInstance map) {
			base.Deserialize(map);

			if (instance != null) {
				instance.GameObject = gameObject;
			}
			
		}
	}
}