using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Narramancer
{
	public class ChoiceButtonTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {

		private string text;
		private GameObject tooltipGameObject;

		public void SetText(string tooltipText) {
			text = tooltipText;
		}

		public void SetToolTipObject(GameObject tooltipGameObject) {
			this.tooltipGameObject = tooltipGameObject;
		}

		public void OnPointerEnter(PointerEventData eventData) {
			var textComponent = tooltipGameObject.GetComponentInChildren<Text>();
			textComponent.text = text;
			tooltipGameObject.SetActive(true);
		}

		public void OnPointerExit(PointerEventData eventData) {
			tooltipGameObject.SetActive(false);
		}
	}
}
