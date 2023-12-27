using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Narramancer {
	public static class AnimationCoroutineExtensions {

		public static void StopCoroutineMaybe(this MonoBehaviour @this, Coroutine coroutine) {
			if (coroutine != null) {
				@this.StopCoroutine(coroutine);
			}
		}

		public static void RestartCoroutine(this MonoBehaviour @this, ref Coroutine coroutine, IEnumerator animation) {
			@this.StopCoroutineMaybe(coroutine);

			coroutine = @this.StartCoroutine(animation);
		}

		public static IEnumerator FadeIn(this CanvasGroup @this, float speed = 10f) {
			while (@this.alpha < 1f) {
				yield return new WaitForEndOfFrame();

				@this.alpha += speed * Time.deltaTime;
			}
			@this.alpha = 1f;
		}

		public static IEnumerator FadeOut(this CanvasGroup @this, float speed = 10f) {
			while (@this.alpha > 0f) {
				yield return new WaitForEndOfFrame();

				@this.alpha -= speed * Time.deltaTime;
			}
			@this.alpha = 0f;
		}

		public static IEnumerator Then(this IEnumerator @this, Action callback) {
			yield return @this;
			callback?.Invoke();
		}

		public static IEnumerator ScrollToBottom(this ScrollRect @this, float speed = 10f) {
			while (@this.verticalNormalizedPosition > 0f) {
				yield return new WaitForEndOfFrame();

				@this.verticalNormalizedPosition -= speed * Time.deltaTime;
			}
			@this.verticalNormalizedPosition = 0f;
		}

		public static IEnumerator ScrollToTop(this ScrollRect @this, float speed = 10f) {
			while (@this.verticalNormalizedPosition < 1f) {
				yield return new WaitForEndOfFrame();

				@this.verticalNormalizedPosition += speed * Time.deltaTime;
			}
			@this.verticalNormalizedPosition = 1f;
		}

	}
}