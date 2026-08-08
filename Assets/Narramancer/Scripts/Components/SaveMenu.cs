using System.Collections;
using System.Collections.Generic;
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

		private void Awake() {
			if (slotPrefab == null) {
				Debug.LogError("slotPrefab is required", this);
				return;
			}
			// Deactivate the template here rather than in Start(): OnEnable() runs before
			// Start() and already instantiates from it.
			slotPrefab.SetActive(false);
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

		public void Save(string saveName) {
			StartCoroutine(SaveRoutine(saveName));
		}

		private IEnumerator SaveRoutine(string saveName) {
			var canvas = GetComponentInParent<Canvas>();

			// Hide the UI so it stays out of the save thumbnail. Disable the Canvas component
			// rather than deactivating its GameObject: this menu lives under that canvas, and
			// deactivating a GameObject terminates its coroutines, so the save would never run.
			if (canvas != null) {
				canvas.enabled = false;
			}

			// The thumbnail reads the frame buffer, which is only valid at the end of a rendered
			// frame. This also gives the canvas a frame to actually disappear.
			yield return new WaitForEndOfFrame();

			try {
				var story = NarramancerSingleton.Instance.PrepareStoryForSave();

				var jsonString = SaveLoadUtilities.SerializeData(story);

				SaveLoadUtilities.WriteSaveData(saveName, jsonString);
			}
			finally {
				if (canvas != null) {
					canvas.enabled = true;
				}
			}

			OnEnable();
		}
	}
}
