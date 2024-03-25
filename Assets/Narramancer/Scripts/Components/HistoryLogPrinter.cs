using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Narramancer {
	public class HistoryLogPrinter : MonoBehaviour {

		[SerializeField]
		GameObject textRowPrefab = default;

		List<GameObject> existingTextRows = new List<GameObject>();

		private void Awake() {
			textRowPrefab.SetActive(false);
		}

		private void ClearExistingRows() {
			foreach( var textRow in existingTextRows) {
				Destroy(textRow);
			}
			existingTextRows.Clear();
		}

		private void CreateTextRows() {
			foreach(var text in NarramancerSingleton.Instance.StoryInstance.TextLogs) {
				var textRow = Instantiate(textRowPrefab, textRowPrefab.transform.parent);
				existingTextRows.Add(textRow);
				textRow.SetActive(true);

				var textComponent = textRow.GetComponent<Text>();
				textComponent.text = text;
			}
		}

		private void OnEnable() {
			ClearExistingRows();
			CreateTextRows();
		}
	}
}
