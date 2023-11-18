using UnityEngine;

namespace Narramancer {

	[CreateAssetMenu(menuName = "Narramancer/Flag", fileName = "New Flag")]
	public class Flag : ScriptableObject {

		[SerializeField]
		private ReferenceList references = new ReferenceList();

	}
}