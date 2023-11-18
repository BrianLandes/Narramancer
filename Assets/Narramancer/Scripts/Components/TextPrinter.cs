using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Narramancer {
	public class TextPrinter : SerializableMonoBehaviour {

		[SerializeField]
		Text textField = default;

		[SerializeField, Min(0.01f)]
		float revealSpeed = 80f;

		[SerializeField]
		GameObject continueIndicator = default;

		[SerializeField]
		CanvasGroup parentCanvasGroup = default;

		[SerializeField, Min(0.01f)]
		float fadeParentSpeed = 10f;


		[SerializeMonoBehaviourField]
		string targetText = "";

		[SerializeMonoBehaviourField]
		Promise promise = default;

		Coroutine revealTextCoroutine = default;
		Coroutine hideParentCoroutine = default;

		public bool IsRevealingText { get { return !textField.text.Equals(targetText, System.StringComparison.Ordinal); } }

		private void Start() {
			if (!valuesOverwrittenByDeserialize) {
				if (revealTextCoroutine == null) {
					parentCanvasGroup.alpha = 0f;
					parentCanvasGroup.gameObject.SetActive(false);
				}
			}
		}

		public void SetText(string text, Action callback) {
			ShowParentCanvas();
			textField.text = "";
			continueIndicator.SetActive(false);
			targetText = text;
			if (revealTextCoroutine != null) {
				StopCoroutine(revealTextCoroutine);
			}
			revealTextCoroutine = StartCoroutine(RevealText(text));
			if (callback != null) {
				promise = new Promise();
				promise.WhenDone(callback);
			}
			else {
				promise = null;
			}

		}

		IEnumerator RevealText(string text) {
			var delay = 1f / revealSpeed;
			for (int ii = 0; ii < text.Length; ii++) {
				yield return new WaitForSeconds(delay);
				var subText = text.Substring(0, ii);
				textField.text = subText;
			}
			textField.text = text;
			continueIndicator.SetActive(true);
		}

		public void OnContinue() {
			if (IsRevealingText) {
				SkipTextReveal();
			}
			else {
				continueIndicator.SetActive(false);
				HideParentCanvas();
				targetText = null;
				promise?.Resolve();
			}
		}

		public void SkipTextReveal() {
			this.StopCoroutineMaybe(revealTextCoroutine);
			textField.text = targetText;
			continueIndicator.SetActive(true);
		}

		public override void Deserialize(StoryInstance map) {
			SkipTextReveal();

			base.Deserialize(map);

			if (targetText.IsNotNullOrEmpty()) {
				textField.text = targetText;
				continueIndicator.SetActive(true);
				parentCanvasGroup.gameObject.SetActive(true);
			}
			else {
				continueIndicator.SetActive(false);
				parentCanvasGroup.gameObject.SetActive(false);
			}
		}

		public void ShowParentCanvas() {

			parentCanvasGroup.gameObject.SetActive(true);

			this.StopCoroutineMaybe(hideParentCoroutine);

			hideParentCoroutine = StartCoroutine(parentCanvasGroup.FadeIn(fadeParentSpeed));

		}


		public void HideParentCanvas() {

			this.StopCoroutineMaybe(hideParentCoroutine);

			hideParentCoroutine = StartCoroutine(
				parentCanvasGroup.FadeOut(fadeParentSpeed)
				.Then(() => {
					parentCanvasGroup.gameObject.SetActive(false);
				})
			);

		}
	}
}