

using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Narramancer {

	public abstract class Singleton<T> : SingletonBase where T : SingletonBase {

		public static T Instance {
			get {
				if (_instance == null) {
					if (SingletonManager.Instance != null) {
						_instance = SingletonManager.Instance.Get<T>();
					}
					else {
						_instance = Resources.LoadAll<T>(string.Empty).FirstOrDefault();
					}
				}
				return _instance;
			}
		}
		private static T _instance = null;

	}


	/// <summary>
	/// The non-generic version of Singleton.
	/// </summary>
	public abstract class SingletonBase : ScriptableObject {

		#region Project Settings Path

		/// <summary> Control where this Singleton appears within the Project Settings. </summary>
		public virtual string GetProjectSettingsPath() {
			var singletonType = this.GetType();

			string niceName = singletonType.Name.Nicify();

			return $"Project/{niceName}";
		}

		#endregion

		#region Virtual Lifecyle Event Handlers

		/// <summary> Used to initially initialize the singleton. Invoked when the game is first started, before any scene is loaded. </summary>
		public virtual void Initialize() {
			// no need to call base when overridden
		}

		/// <summary> Invoked at the same time as MonoBehaviour.Start().  </summary>
		public virtual void OnGameStart() {
			// no need to call base when overridden
		}

		/// <summary> Invoked when the SingletonManager is destroyed. </summary>
		public virtual void OnGameDestroyed() {
			// no need to call base when overridden
		}

		/// <summary> Invoked at the same time and in the same way as MonoBehaviour.FixedUpdate(). </summary>
		public virtual void OnFixedUpdate() {
			// no need to call base when overridden
		}

		/// <summary> Invoked at the same time and in the same way as MonoBehaviour.Update(). </summary>
		public virtual void OnUpdate() {
			// no need to call base when overridden
		}

		/// <summary> Invoked at the same time and in the same way as MonoBehaviour.LateUpdate(). </summary>
		public virtual void OnLateUpdate() {
			// no need to call base when overridden
		}

		/// <summary> Invoked if a Unity Scene is Unloaded. </summary>
		public virtual void OnSceneUnloaded(Scene scene) {
			// no need to call base when overridden
		}

		/// <summary> Invoked if a Unity Scene is Loaded. </summary>
		public virtual void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode) {
			// no need to call base when overridden
		}

		public virtual void OnPreprocessBuild() {
			// no need to call base when overridden
		}

		#endregion

		#region Coroutines

		/// <inheritdoc cref="MonoBehaviour.StartCoroutine(IEnumerator)"/>
		protected Coroutine StartCoroutine(IEnumerator routine) {
			return SingletonManager.Instance.StartCoroutine(routine);
		}

		/// <inheritdoc cref="MonoBehaviour.StartCoroutine(string)"/>
		protected Coroutine StartCoroutine(string methodName) {
			return SingletonManager.Instance.StartCoroutine(methodName);
		}

		/// <inheritdoc cref="MonoBehaviour.StopCoroutine(Coroutine)"/>
		protected void StopCoroutine(Coroutine routine) {
			SingletonManager.Instance.StopCoroutine(routine);
		}

		/// <inheritdoc cref="MonoBehaviour.StopCoroutine(IEnumerator)"/>
		protected void StopCoroutine(IEnumerator routine) {
			SingletonManager.Instance.StopCoroutine(routine);
		}

		/// <inheritdoc cref="MonoBehaviour.StopCoroutine(string)"/>
		protected void StopCoroutine(string methodName) {
			SingletonManager.Instance.StopCoroutine(methodName);
		}

		/// <inheritdoc cref="MonoBehaviour.StopAllCoroutines"/>
		protected void StopAllCoroutines() {
			SingletonManager.Instance.StopAllCoroutines();
		}

		#endregion
	}
}