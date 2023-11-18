using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Narramancer {
	public class LoadMenu : MonoBehaviour {

		[SerializeField]
		private GameObject slotPrefab = default;

		[SerializeField]
		private Image slotThumbnail = default;

		[SerializeField]
		private Transform slotContainer = default;

		List<GameObject> currentSlots = new List<GameObject>();

		private void Start() {
			slotPrefab.SetActive(false);
			if (slotPrefab == null) {
				Debug.LogError("slotPrefab is required", this);
			}
		}

		public void ClearSlots() {
			foreach (var currentSlot in currentSlots) {
				Destroy(currentSlot);
			}
			currentSlots.Clear();
		}
		private void OnEnable() {
			ClearSlots();
			var nameWrapperPairs = SaveLoadUtilities.GetSaveDataInWrappers();

			foreach (var pair in nameWrapperPairs) {
				var saveName = pair.Item1;
				var wrapper = pair.Item2;
				CreateSlot(wrapper.title, saveName);
			}
		}

		public void CreateSlot(string title, string saveName) {
			var newSlot = Instantiate(slotPrefab, slotContainer);
			newSlot.SetActive(true);

			var textComponent = newSlot.GetComponentInChildren<Text>();
			textComponent.text = title;

			var buttonComponent = newSlot.GetComponentInChildren<Button>();
			buttonComponent.onClick.AddListener(() => Load(saveName));

			currentSlots.Add(newSlot);
		}
		public void Load(string saveName) {

			var story = SaveLoadUtilities.ReadAndDeserializeSaveData<StoryInstance>(saveName);

			NarramancerSingleton.Instance.LoadStory(story);
		}


	}
}
