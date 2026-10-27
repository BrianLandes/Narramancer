using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Narramancer {
	[DefaultExecutionOrder(-100)]
	public class CreateNounForGameObject : SerializableMonoBehaviour, IInstancable {

		[SerializeField]
		private string displayName = "";
		public string DisplayName => displayName;

		[SerializeField]
		private NounUID uid = new NounUID();
		public NounUID ID => uid;

		[SerializeField]
		private bool destroyNounWithGameObject = false;

		[SerializeField]
		private List<PropertyAssignment> properties = new List<PropertyAssignment>();
		public IEnumerable<PropertyAssignment> Properties => properties;

		[SerializeField]
		private List<StatAssignment> stats = new List<StatAssignment>();
		public IEnumerable<StatAssignment> Stats => stats;

		[SerializeField]
		private List<RelationshipAssignment> relationships = new List<RelationshipAssignment>();
		public IEnumerable<RelationshipAssignment> Relationships => relationships;

		[SerializeField]
		private Blackboard startingBlackboard = new Blackboard();
		public Blackboard Blackboard => startingBlackboard;

		public NounInstance Instance { get; private set; }

		private void Start() {
			EnsureInstance();
		}

		/// <summary>
		/// This object's noun, creating it now if <c>Start</c> has not run yet.
		/// <para>
		/// Anything that asked for the noun during another component's <c>Awake</c> or <c>Start</c> used to get
		/// null, depending on an execution order nobody had set. The <c>[DefaultExecutionOrder]</c> makes the
		/// usual case come out right; this makes the rest of them come out right too. Idempotent — the noun is
		/// created once, however many callers race for it.
		/// </para>
		/// </summary>
		public NounInstance EnsureInstance() {
			if (Instance == null && !valuesOverwrittenByDeserialize) {
				Instance = NarramancerSingleton.Instance.CreateInstance(this);
			}
			if (Instance != null) {
				Instance.GameObject = gameObject;
			}
			return Instance;
		}

		public override void Serialize(StoryInstance map) {
			base.Serialize(map);

			map.SaveTable.Set(Key("Instance"), Instance);
		}

		public override void Deserialize(StoryInstance map) {
			base.Deserialize(map);
			if (!valuesOverwrittenByDeserialize) {
				return;
			}

			Instance = map.SaveTable.Get<NounInstance>(Key("Instance"));
			if (Instance != null) {
				Instance.GameObject = gameObject;
			}
		}

#if UNITY_EDITOR
		[MenuItem("GameObject/Narramancer/Create Noun For GameObject", false, 10)]
		static void CreateGameObject(MenuCommand menuCommand) {

			GameObject gameObject = new GameObject("Create Noun");
			gameObject.AddComponent<CreateNounForGameObject>();

			GameObjectUtility.SetParentAndAlign(gameObject, menuCommand.context as GameObject);

			Undo.RegisterCreatedObjectUndo(gameObject, "Create " + gameObject.name);
			Selection.activeObject = gameObject;
		}
#endif

		public override void OnDestroy() {
			base.OnDestroy();
			if ( destroyNounWithGameObject) {
				NarramancerSingleton.Instance.RemoveInstance(Instance);
			}
		}
	}

}
