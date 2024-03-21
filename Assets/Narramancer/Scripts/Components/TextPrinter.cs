using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace Narramancer {

	public class TextPrinter : SerializableMonoBehaviour {

		public bool isMainTextPrinter = true;

		[SerializeField]
		protected Text textField = default;

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

		protected Coroutine revealTextCoroutine = default;
		Coroutine hideParentCoroutine = default;

		public bool IsRevealingText { get; set; }

		void Start() {
			if (!valuesOverwrittenByDeserialize) {
				if (revealTextCoroutine == null) {
					parentCanvasGroup.alpha = 0f;
					parentCanvasGroup.gameObject.SetActive(false);
				}
			}
		}

		public virtual void SetText(string text, Action callback, bool clearPreviousText = true) {
			ShowParentCanvas();
			var previousText = string.Empty;
			if (!clearPreviousText && targetText.IsNotNullOrEmpty()) {
				previousText = targetText + "\n";
			}
			textField.text = "";
			continueIndicator?.SetActive(false);
			targetText = previousText + text;

			this.RestartCoroutine(ref revealTextCoroutine, RevealText(text, previousText));

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

		IEnumerator RevealText(string text, string seenText = "") {

			var tagsInText = tagRegex.Matches(text);

			IsRevealingText = true;

			var position = 0f;
			var textIndex = 0;
			do {

				position += Time.deltaTime * revealSpeed;
				textIndex = Mathf.Min(text.Length - 1, Mathf.FloorToInt(position));
				#region Skip over whitespace
				if (text[textIndex] == ' ') {
					textIndex++;
					position += 1;
				}
				#endregion

				#region Skip over any tags
				var intersectedMatch = tagsInText.FirstOrDefault(match => match.Index <= textIndex && match.Index + match.Length > textIndex);
				if (intersectedMatch != null) {
					textIndex += intersectedMatch.Length;
					position += intersectedMatch.Length;
				}
				#endregion

				if (textIndex >= text.Length) {
					break;
				}
				var visibleText = text.Substring(0, textIndex);
				var invisibleText = text.Substring(textIndex);

				#region Account for closing tags that we haven't yet revealed
				var openTagsInVisible = openTagRegex.Matches(visibleText);
				var closeTagsInVisible = closeTagRegex.Matches(visibleText);
				var difference = openTagsInVisible.Count - closeTagsInVisible.Count;
				if (difference > 0) {
					var openTagsInInvisible = openTagRegex.Matches(invisibleText);
					var closeTagsInInvisible = closeTagRegex.Matches(invisibleText);
					var firstCloseTagIndex = closeTagsInInvisible.OrderBy(match => match.Index).FirstOrDefault()?.Index;
					var nestedOpenTagsInInvisible = openTagsInInvisible.Count(match => match.Index < firstCloseTagIndex);

					for (var jj = 0; jj < difference && nestedOpenTagsInInvisible + jj < closeTagsInInvisible.Count; jj++) {
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

				textField.text = seenText + subText;

				yield return new WaitForEndOfFrame();

			} while (textIndex < text.Length - 1);

			textField.text = seenText + text;
			continueIndicator?.SetActive(true);
			IsRevealingText = false;
		}

		public virtual void OnContinue() {
			if (IsRevealingText) {
				SkipTextReveal();
			}
			else {
				continueIndicator?.SetActive(false);
				HideParentCanvas();
				targetText = null;
				promise?.Resolve();
			}
		}

		public void SkipTextReveal() {
			this.StopCoroutineMaybe(revealTextCoroutine);
			textField.text = targetText;
			continueIndicator?.SetActive(true);
			IsRevealingText = false;
		}

		public override void Deserialize(StoryInstance map) {
			SkipTextReveal();

			base.Deserialize(map);

			if (targetText.IsNotNullOrEmpty()) {
				textField.text = targetText;
				continueIndicator?.SetActive(true);
				parentCanvasGroup.gameObject.SetActive(true);
			}
			else {
				continueIndicator?.SetActive(false);
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

#if !UNITY_2021_2_OR_NEWER
	public static class RegexExtensions {

		public static Match FirstOrDefault(this MatchCollection source, Func<Match, bool> predicate) {
			for( var ii = 0; ii < source.Count; ii ++ ) {
				var match = source[ii];
				var result = predicate(match);
				if (result) {
					return match;
				}
			}
			return null;
		}

		public static IEnumerable<Match> OrderBy(this MatchCollection source, Func<Match, int> keySelector) {
			var list = new List<Match>();
			var enumerator = source.GetEnumerator();
			while (enumerator.MoveNext()) {
				list.Add(enumerator.Current as Match);
			}
			return list.OrderBy(keySelector);
		}

		public static int Count(this MatchCollection source, Func<Match, bool> predicate) {
			var count = 0;
			for (var ii = 0; ii < source.Count; ii++) {
				var match = source[ii];
				var result = predicate(match);
				if (result) {
					count += 1;
				}
			}
			return count;
		}
	}
#endif
}