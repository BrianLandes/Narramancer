﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Narramancer {

	public abstract class SerializableMonoBehaviour : MonoBehaviour, ISerializableMonoBehaviour {

		protected bool valuesOverwrittenByDeserialize = false;

		public virtual string UniqueKey => transform.FullPath();

		public virtual void Awake() {
			NarramancerSingleton.Instance.Register(this);
		}

		public virtual void OnDestroy() {
			NarramancerSingleton.Instance.Unregister(this);
		}

		public string Key(string key) {
			return $"{key} {UniqueKey}";
		}

		public virtual void Serialize(StoryInstance story) {
			var type = this.GetType();

			var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

			foreach (var field in fields) {
				foreach (var attribute in field.GetCustomAttributes()) {
					if (attribute is SerializeMonoBehaviourFieldAttribute) {

						if (field.FieldType == typeof(Promise)) {
							var value = (Promise)field.GetValue(this);
							story.Promises[Key(field.Name)] = value;
						}
						else
						if (field.FieldType == typeof(NodeRunner)) {
							var value = (NodeRunner)field.GetValue(this);
							story.NodeRunners[Key(field.Name)] = value;
						}
						else {
							var value = field.GetValue(this);
							story.Blackboard.Set(Key(field.Name), value, field.FieldType);

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
							var promise = story.Promises[Key(field.Name)]; // TODO: fail safe
							field.SetValue(this, promise);
						}
						else
						if (field.FieldType == typeof(NodeRunner)) {
							var runner = story.NodeRunners[Key(field.Name)]; // TODO: fail safe
							field.SetValue(this, runner);
						}
						else {
							var value = story.Blackboard.Get(Key(field.Name), field.FieldType);
							field.SetValue(this, value);
						}
					}
				}
			}
		}
	}
}