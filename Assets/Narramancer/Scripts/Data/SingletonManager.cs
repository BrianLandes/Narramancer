
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Narramancer {

	/// <summary>
	/// Acts as a Manager for all Singletons. <br/>
	/// Creates and initializes a Manager object on game start. <br/>
	/// Finds and initializes all Singletons. <br/>
	/// Propogates game events, frame updates, and scene changes down to all Singletons. <br/>
	/// </summary>
	[DefaultExecutionOrder(-9999)]
	public class SingletonManager : MonoBehaviour {

		#region Fields and Properties

		public static SingletonManager Instance { get; protected set; }

		private SingletonBase[] singletons = null;

		#endregion

		#region Setup and Manager Initialization

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void InitializeManager() {
			InstantiateManagerObject();
			InitializeAllSingletons();
			ListenToSceneChanges();
		}

		private static void InstantiateManagerObject() {
			var managerObject = new GameObject("Singleton System Manager");

			var managerComponent = managerObject.AddComponent<SingletonManager>();

			DontDestroyOnLoad(managerObject);

			Instance = managerComponent;
		}

		private static void InitializeAllSingletons() {
			foreach( var singleton in Instance.GetAll() ) {
				singleton.Initialize();
			}
		}

		private static void ListenToSceneChanges() {
			SceneManager.sceneLoaded += Instance.OnSceneLoaded;

			SceneManager.sceneUnloaded += Instance.OnSceneUnloaded;
		}

		#endregion

		#region MonoBehaviour Lifecycle

		private void Start() {
			foreach (var singleton in GetAll()) {
				singleton.OnGameStart();
			}
		}

		private void OnDestroy() {
			foreach (var singleton in GetAll()) {
				singleton.OnGameDestroyed();
			}
		}

		#endregion

		#region MonoBehaviour Update

		private void FixedUpdate() {
			foreach (var singleton in GetAll()) {
				singleton.OnFixedUpdate();
			}
		}

		private void Update() {
			foreach (var singleton in GetAll()) {
				singleton.OnUpdate();
			}
		}

		private void LateUpdate() {
			foreach (var singleton in GetAll()) {
				singleton.OnLateUpdate();
			}
		}

		#endregion

		#region Scene Change Methods

		private void OnSceneUnloaded(Scene scene) {
			foreach (var singleton in GetAll()) {
				singleton.OnSceneUnloaded(scene);
			}
		}

		private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode) {
			foreach (var singleton in GetAll()) {
				singleton.OnSceneLoaded(scene, loadSceneMode);
			}
		}

		#endregion

		#region Get Singleton Methods

		/// <summary>
		/// Finds and returns all known Singletons.
		/// </summary>
		public IEnumerable<SingletonBase> GetAll() {
			if (singletons == null) {
				singletons = Resources.LoadAll<SingletonBase>(string.Empty).ToArray();
			}
			return singletons;
		}

		/// <summary>
		/// Finds and returns the first Singleton instance with the given Type.
		/// </summary>
		/// <returns>The first Singleton of the given type OR null if none was found.</returns>
		public T Get<T>() where T : SingletonBase {
			return GetAll()
				.FirstOrDefault(singleton => typeof(T).IsAssignableFrom(singleton.GetType()))
				as T;
		}

		#endregion
	}
}
