using System.Linq;

namespace Narramancer {

	public class SerializeNounInstanceReference : SerializableMonoBehaviour {

		[SerializeMonoBehaviourField]
		private NounInstance instance;

		public NounInstance GetInstance() {
			return NarramancerSingleton.Instance.GetInstances().FirstOrDefault(instance => instance.GameObject == gameObject);
			;
		}

		public override void Serialize(StoryInstance story) {

			if (instance == null) {
				instance = GetInstance();
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