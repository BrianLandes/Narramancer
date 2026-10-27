using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Narramancer {

	public abstract class SerializableMonoBehaviour : MonoBehaviour, ISerializableMonoBehaviour {

		/// <summary>
		/// True once <see cref="Deserialize"/> has found state saved for this component. Stays false when the
		/// save holds nothing for it — an object added to the scene by a later patch, say — so the component
		/// initializes from the scene as if the game had just started, rather than being reset to defaults.
		/// Overrides of <see cref="Deserialize"/> should return early when it is false.
		/// </summary>
		protected bool valuesOverwrittenByDeserialize = false;

		/// <summary>
		/// Written by every <see cref="Serialize"/>, so a load can tell "saved, and the value was the default"
		/// from "never saved at all". Without it, a component the save knew nothing about read every value back
		/// as its type's default: a <see cref="SerializeActive"/> hid its object, <see cref="SerializeText"/>
		/// blanked its text, and <see cref="SerializeTransform"/> threw — which, inside the load loop, abandoned
		/// the load for every component after it.
		/// </summary>
		private const string SavedMarker = "$saved";

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
			story.SaveTable.SetBool(Key(SavedMarker), true);

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

			if (!story.SaveTable.HasBool(Key(SavedMarker))) {
				return;
			}
			story.SaveTable.RemoveBool(Key(SavedMarker));

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
						else
						if (story.SaveTable.Contains(Key(field.Name), field.FieldType)) {
							// A field added since the save was written keeps its initializer rather than its type's default.
							var value = story.SaveTable.GetAndRemove(Key(field.Name), field.FieldType);
							field.SetValue(this, value);
						}
					}
				}
			}
		}
	}
}