using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Narramancer {

	public abstract class SerializableMonoBehaviour : MonoBehaviour, ISerializableMonoBehaviour {

		protected bool valuesOverwrittenByDeserialize = false;

		public virtual void Awake() {
			NarramancerSingleton.Instance.Register(this);
		}

		public virtual void OnDestroy() {
			NarramancerSingleton.Instance.Unregister(this);
		}

		[SerializeField]
		[HideInInspector]
		private Saveable saveable = default;

		/// <summary>
		/// The <see cref="Saveable"/> that gives this object its identity. Cached, because <see cref="Key"/>
		/// runs once per saved field.
		/// </summary>
		protected Saveable Saveable {
			get {
				if (saveable == null) {
					saveable = Saveable.Of(this);
				}
				return saveable;
			}
		}

		/// <summary>
		/// Where this component's state is filed in the save.
		/// <para>
		/// Keyed by the object's minted guid plus the component type. It used to be
		/// <c>"{key} {transform.FullPath()}[{componentIndex}]"</c>, which meant renaming the object,
		/// reparenting it, reordering its components, or two siblings sharing a name all changed the key —
		/// and a key that no longer matches does not error, it reads back as the default. The save loaded and
		/// the object was quietly wrong.
		/// </para>
		/// <para>
		/// The component type is part of the key so two different savers on one object do not collide;
		/// <see cref="DisallowMultipleComponent"/> is not enforced on these, so a name alone would not be
		/// enough if someone added two of the same type.
		/// </para>
		/// </summary>
		public virtual string Key(string key) {
			var identity = Saveable;
			if (identity == null) {
				// Saveable.Of has already said which object is missing one. Fall back to the old path key so
				// the rest of the save still round-trips rather than colliding on a single empty guid.
				return $"{key} {transform.FullPath()}[{transform.IndexOfComponent(this)}]";
			}
			return $"{identity.EnsureGuid()}:{GetType().Name}:{key}";
		}

		public virtual void Serialize(StoryInstance story) {
			var type = this.GetType();

			var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

			foreach (var field in fields) {
				foreach (var attribute in field.GetCustomAttributes()) {
					if (attribute is SerializeMonoBehaviourFieldAttribute) {

						if (field.FieldType == typeof(Promise)) {
							var value = (Promise)field.GetValue(this);
							if (value != null) {
								story.Promises[Key(field.Name)] = value;
							}
						}
						else
						if (field.FieldType == typeof(NodeRunner)) {
							var value = (NodeRunner)field.GetValue(this);
							if (value != null) {
								story.NodeRunners[Key(field.Name)] = value;
							}
						}
						else {
							var value = field.GetValue(this);
							story.SaveTable.Set(Key(field.Name), value, field.FieldType);

						}
					}
				}
			}
		}

		public virtual void Deserialize(StoryInstance story) {

			valuesOverwrittenByDeserialize = true;

			var type = this.GetType();

			var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

			foreach (var field in fields) {
				foreach (var attribute in field.GetCustomAttributes()) {
					if (attribute is SerializeMonoBehaviourFieldAttribute) {

						if (field.FieldType == typeof(Promise)) {
							if (story.Promises.TryGetValue(Key(field.Name), out var promise)) {
								field.SetValue(this, promise);
							}
						}
						else
						if (field.FieldType == typeof(NodeRunner)) {
							if (story.NodeRunners.TryGetValue(Key(field.Name), out var runner)) {
								field.SetValue(this, runner);
							}
						}
						else {
							var value = story.SaveTable.GetAndRemove(Key(field.Name), field.FieldType);
							field.SetValue(this, value);
						}
					}
				}
			}
		}
	}
}