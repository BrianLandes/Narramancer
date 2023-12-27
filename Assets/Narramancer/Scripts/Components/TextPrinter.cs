using System;
using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace Narramancer {

	public class TextPrinter : SerializableMonoBehaviour {

		[SerializeField]
		Text textField = default;

		[SerializeField, Min(0.01f)]
		float revealSpeed = 80f;

		[SerializeField]
		protected GameObject continueIndicator = default;

		[SerializeField]
		protected CanvasGroup parentCanvasGroup = default;

		[SerializeField, Min(0.01f)]
		float fadeParentSpeed = 10f;


		[SerializeMonoBehaviourField]
		protected string targetText = "";

		[SerializeMonoBehaviourField]
		protected Promise promise = default;

		Coroutine revealTextCoroutine = default;
		Coroutine hideParentCoroutine = default;

		public bool IsRevealingText { get; set; }

		private void Start() {
			if (!valuesOverwrittenByDeserialize) {
				if (revealTextCoroutine == null) {
					parentCanvasGroup.alpha = 0f;
					parentCanvasGroup.gameObject.SetActive(false);
				}
			}
		}

		public virtual void SetText(string text, Action callback) {
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

		Regex tagRegex = new Regex(@"<[/a-zA-Z0-9=#]*>");
		Regex openTagRegex = new Regex(@"<[0-9a-zA-Z=#]*>");
		Regex closeTagRegex = new Regex(@"<\/[0-9a-zA-Z=#]*>");
		Regex colorStartRegex = new Regex(@"<color=[#0-9a-z]*>");
		Regex colorEndRegex = new Regex(@"</color>");

		string RemoveMatch( string text, Match match) {
			return text.Substring(0, match.Index) + text.Substring(match.Index + match.Length);
		}

		IEnumerator RevealText(string text) {

			var tagsInText = tagRegex.Matches(text);

			IsRevealingText = true;

			var position = 0f;
			var ii = 0;
			do {

				position += Time.deltaTime * revealSpeed;
				ii = Mathf.Min(text.Length-1, Mathf.FloorToInt(position));
				#region Skip over whitespace
				if (text[ii] == ' ') {
					ii++;
					position += 1;
				}
				#endregion

				#region Skip over any tags
				var intersectedMatch = tagsInText.FirstOrDefault(match => match.Index <= ii && match.Index + match.Length > ii);
				if (intersectedMatch != null) {
					ii += intersectedMatch.Length;
					position += intersectedMatch.Length;
				}
				#endregion

				var visibleText = text.Substring(0, ii);
				var invisibleText = text.Substring(ii);

				#region Account for closing tags that we haven't yet revealed
				var openTagsInVisible = openTagRegex.Matches(visibleText);
				var closeTagsInVisible = closeTagRegex.Matches(visibleText);
				var difference = openTagsInVisible.Count - closeTagsInVisible.Count;
				if (difference > 0) {
					var openTagsInInvisible = openTagRegex.Matches(invisibleText);
					var closeTagsInInvisible = closeTagRegex.Matches(invisibleText);
					var firstCloseTagIndex = closeTagsInInvisible.OrderBy(match => match.Index).FirstOrDefault()?.Index;
					var nestedOpenTagsInInvisible = openTagsInInvisible.Count(match => match.Index < firstCloseTagIndex);

					for (var jj = 0; jj < difference; jj++) {
						var match = closeTagsInInvisible[nestedOpenTagsInInvisible + jj];
						var tag = invisibleText.Substring(match.Index, match.Length);
						visibleText += tag;
					}
				}

				#endregion

				#region Remove All Color Tags From Invisible Text

				while (tagRegex.IsMatch(invisibleText)) {
					var match = tagRegex.Match(invisibleText);
					invisibleText = invisibleText.Substring(0, match.Index) + invisibleText.Substring(match.Index + match.Length);
				}
				#endregion

				var subText = $"{visibleText}<color=black>{invisibleText}</color>";

				textField.text = subText;

				yield return new WaitForEndOfFrame();

			} while (ii < text.Length-1);

			textField.text = text;
			continueIndicator.SetActive(true);
			IsRevealingText = false;
		}

		public virtual void OnContinue() {
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
			IsRevealingText = false;
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