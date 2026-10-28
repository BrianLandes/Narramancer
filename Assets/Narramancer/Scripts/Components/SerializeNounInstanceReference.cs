using System.Linq;

namespace Narramancer {

	public class SerializeNounInstanceReference : SerializableMonoBehaviour {

		[Save]
		private NounInstance instance;

		public NounInstance GetInstance() {
			var existing = NarramancerSingleton.Instance.GetInstances().FirstOrDefault(instance => instance.GameObject == gameObject);
			if (existing == null && TryGetComponent<CreateNounForGameObject>(out var creator)) {
				// Asked before the creator's Start ran — make it now rather than answer null.
				existing = creator.EnsureInstance();
			}
			return existing;
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