using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace Narramancer {
	public class SaveMenu : MonoBehaviour {

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
				CreateSlot(wrapper.title, saveName, wrapper.thumbnail);
			}
		}

		private Transform GetThumbnailChild(GameObject gameObject) {
			var prefabPath = slotPrefab.transform.FullPath();
			var prefabThumbnailPath = slotThumbnail.transform.FullPath();
			var path = prefabThumbnailPath.Replace(prefabPath, "");
			if (path[0] == '/') {
				path = path.Substring(1);
			}
			var child = gameObject.transform.Find(path);
			return child;
		}

		public void CreateSlot(string title, string saveName, string thumbnailString) {
			var newSlot = Instantiate(slotPrefab, slotContainer);
			newSlot.SetActive(true);
			var textComponent = newSlot.GetComponentInChildren<Text>();
			textComponent.text = title;

			var buttonComponent = newSlot.GetComponentInChildren<Button>();
			buttonComponent.onClick.AddListener(() => Save(saveName));

			var thumbnailChild = GetThumbnailChild(newSlot);
			var imageComponent = thumbnailChild.GetComponent<Image>();
			var thumbnailTexture = SaveLoadUtilities.DeserializeThumbnail(thumbnailString);
			imageComponent.sprite = Sprite.Create(thumbnailTexture, new Rect(0, 0, thumbnailTexture.width, thumbnailTexture.height), Vector2.zero);

			currentSlots.Add(newSlot);
		}

		public void CreateNewSave() {

			var saveName = "SaveSlot_" + (SaveLoadUtilities.CountSaveData() + 1).ToString("D3");
			Save(saveName);
		}

		public async void Save(string saveName) {
			var canvas = GetComponentInParent<Canvas>();
			canvas?.gameObject.SetActive(false);

			await Task.Delay(1);

			var story = NarramancerSingleton.Instance.PrepareStoryForSave();

			var jsonString = SaveLoadUtilities.SerializeData(story);

			SaveLoadUtilities.WriteSaveData(saveName, jsonString);

			canvas?.gameObject.SetActive(true);

			OnEnable();
		}
	}
}
