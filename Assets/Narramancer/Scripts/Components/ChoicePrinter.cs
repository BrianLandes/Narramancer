using Narramancer.SerializableActionHelper;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace Narramancer {

	public interface IChoicePrinter {

		void ClearChoices();

		void AddChoice(string displayText, Action callbackAction);

		void AddDisabledChoice(string displayText);

		void ShowChoices();
	}

	public class ChoicePrinter : SerializableMonoBehaviour, IChoicePrinter {


		[SerializeField]
		GameObject choiceButtonPrefab = default;

		[SerializeField]
		Transform content = default;

		[SerializeField]
		CanvasGroup parentCanvasGroup = default;

		[SerializeField, Min(0.01f)]
		float fadeParentSpeed = 10f;



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
		List<GameObject> fadingGameObjects = new List<GameObject>();

		Coroutine hideParentCoroutine;

		private void Start() {
			choiceButtonPrefab.SetActive(false);
		}

		public void ClearChoices() {
			HideParentCanvas();
			foreach (var choiceGameObject in fadingGameObjects) {
				Destroy(choiceGameObject);
			}
			fadingGameObjects.Clear();
			foreach (var choiceGameObject in choiceGameObjects) {
				Destroy(choiceGameObject, 1f);
				fadingGameObjects.Add(choiceGameObject);
			}
			choiceGameObjects.Clear();

			choices.Clear();

			parentCanvasGroup.interactable = false;
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
				var newChoiceObject = Instantiate(choiceButtonPrefab, content);
				newChoiceObject.name = choice.displayText;

				var button = newChoiceObject.GetComponentInChildren<Button>();
				button.onClick.AddListener(() => {
					ClearChoices();
					choice.callback.Action();
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
		}

		public override void Serialize(StoryInstance map) {
			base.Serialize(map);
			if (showingChoices) {
				map.SaveTable.SetObject("choices", choices);
			}
		}

		public override void Deserialize(StoryInstance map) {
			ClearChoices();

			base.Deserialize(map);

			if (showingChoices) {
				choices = map.SaveTable.GetObject("choices") as List<VisibleChoice>;
				ShowChoices();
			}
		}


		public void ShowParentCanvas() {
			parentCanvasGroup.gameObject.SetActive(true);

			this.RestartCoroutine(ref hideParentCoroutine, parentCanvasGroup.FadeIn(fadeParentSpeed));
		}


		public void HideParentCanvas() {
			this.StopCoroutineMaybe(hideParentCoroutine);

			hideParentCoroutine = StartCoroutine(parentCanvasGroup.FadeOut(fadeParentSpeed));
		}

		public static IChoicePrinter GetChoicePrinter() {
			var choicePrinter = GameObjectExtensions.FindObjectsOfType<ChoicePrinter>(true).FirstOrDefault();
			if (choicePrinter != null) {
				return choicePrinter;
			}
			var textAndChoicePrinter = GameObjectExtensions.FindObjectsOfType<TextAndChoicePrinter>(true).FirstOrDefault();
			if (textAndChoicePrinter != null) {
				return textAndChoicePrinter;
			}
			return null;
		}

	}
}