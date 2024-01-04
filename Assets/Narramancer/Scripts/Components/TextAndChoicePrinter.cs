using Narramancer.SerializableActionHelper;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Narramancer {
	public class TextAndChoicePrinter : TextPrinter, IChoicePrinter {


		[SerializeField]
		GameObject choiceButtonPrefab = default;

		[SerializeField]
		Transform choiceContent = default;

		[SerializeField]
		ScrollRect scrollRect = default;

		[SerializeField]
		float scrollToBottomSpeed = 10f;


		[Serializable]
		public class VisibleChoice {
			public string displayText;
			public bool enabled = true;
			public SerializableAction callback;
		}

		[SerializeField]
		List<VisibleChoice> choices = new List<VisibleChoice>();

		[SerializeMonoBehaviourField]
		bool showingChoices = false;

		List<GameObject> choiceGameObjects = new List<GameObject>();

		bool dragging = false;

		Coroutine scrollCoroutine = default;

		void Start() {
			choiceButtonPrefab.SetActive(false);
			if (!valuesOverwrittenByDeserialize) {
				textField.text = targetText = string.Empty;
				continueIndicator.SetActive(false);
			}
		}

		#region Text Printer

		public override void SetText(string text, Action callback, bool clearPreviousText = true) {
			this.StopCoroutineMaybe( scrollCoroutine);
			base.SetText(text, callback, clearPreviousText);

			Canvas.ForceUpdateCanvases();
			if (clearPreviousText) {
				scrollRect.verticalNormalizedPosition = 1f;
			}
			else {
				this.RestartCoroutine(ref scrollCoroutine, scrollRect.ScrollToBottom() );
			}
		}

		public override void OnContinue() {
			if (IsRevealingText) {
				SkipTextReveal();
			}
			else
			if (showingChoices) {
				// nothing
			}
			else
			if (dragging) {
				// nothing
			}
			else {
				continueIndicator.SetActive(false);

				var promise = this.promise;
				this.promise = null;
				promise?.Resolve();
			}
		}

		#endregion

		#region Choice Printer

		public void ClearChoices() {

			foreach (var choiceGameObject in choiceGameObjects) {
				Destroy(choiceGameObject);
			}
			choiceGameObjects.Clear();

			choices.Clear();

			showingChoices = false;
		}

		public void AddChoice(string displayText, Action callbackAction) {
			var newChoice = new VisibleChoice() {
				displayText = displayText,
				callback = new SerializableAction(callbackAction)
			};

			choices.Add(newChoice);
		}

		public void AddDisabledChoice(string displayText) {
			var newChoice = new VisibleChoice() {
				displayText = displayText,
				enabled = false
			};

			choices.Add(newChoice);
		}

		public void ShowChoices() {
			foreach (var choice in choices) {
				var newChoiceObject = Instantiate(choiceButtonPrefab, choiceContent);
				newChoiceObject.name = choice.displayText;

				var button = newChoiceObject.GetComponentInChildren<Button>();
				button.onClick.AddListener(() => {
					if (!dragging) {
						ClearChoices();
						choice.callback.Action();
					}
				
				});
				button.interactable = choice.enabled;

				var text = newChoiceObject.GetComponentInChildren<Text>();
				text.text = choice.displayText;

				newChoiceObject.SetActive(true);

				choiceGameObjects.Add(newChoiceObject);
			}
			showingChoices = true;
			parentCanvasGroup.interactable = true;
			ShowParentCanvas();

			Canvas.ForceUpdateCanvases();

			this.RestartCoroutine(ref scrollCoroutine, scrollRect.ScrollToBottom(scrollToBottomSpeed));

		}

		public override void Serialize(StoryInstance map) {
			base.Serialize(map);
			if (showingChoices) {
				map.SaveTable.SetObject(nameof(choices), choices);
			}
		}

		public override void Deserialize(StoryInstance map) {
			ClearChoices();

			base.Deserialize(map);

			if (showingChoices) {
				choices = map.SaveTable.GetObject(nameof(choices)) as List<VisibleChoice>;
				ShowChoices();
				continueIndicator.SetActive(false);
			}
		}

		#endregion

		#region Scroll View + Event Trigger

		public void BeginDrag() {
			dragging = true;
		}

		public void EndDrag() {
			dragging = false;
		}

		#endregion
	}
}