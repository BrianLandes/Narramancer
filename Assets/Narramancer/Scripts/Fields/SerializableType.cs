using System;
using System.Collections.Generic;
using UnityEngine;

namespace Narramancer {

	/// <summary>
	/// A wrapper that allows System.Type to be selected and serialized in the Unity Inspector.
	/// </summary>
	[Serializable]
	public class SerializableType {

		[SerializeField]
		private string typeName;
		public static string TypeFieldName => nameof(typeName);

		[SerializeField]
		private bool list = false;
		public bool List { get => list; set { list = value; ApplyChanges(); } }

		/// <summary>
		/// Allows Nodes and other GUIs to respond to the type being changed.
		/// </summary>
		public event Action OnChanged;

		public Func<Type, bool> typeFilter = null;

		public bool canBeList = true;

		public Type Type {
			get {
				if (typeName.IsNullOrEmpty()) {
					cachedType = null;
					return null;
				}
				if (list) {
					if (cachedType == null || !(cachedType.AssemblyQualifiedName.StartsWith("System.Collections.Generic.List" , StringComparison.Ordinal) && cachedType.AssemblyQualifiedName.Contains( typeName))) {
						cachedType = typeof(List<>).MakeGenericType(Type.GetType(typeName, true));
					}
				}
				else {
					if ( cachedType == null || !string.Equals( cachedType.AssemblyQualifiedName, typeName, StringComparison.Ordinal) ) {
						cachedType = AssemblyUtilities.GetType(typeName);
						if (cachedType != null) {
							typeName = cachedType.AssemblyQualifiedName;
						}
					}
				}
				return cachedType;
			}
			set {
				typeName = value.AssemblyQualifiedName;
				ApplyChanges();
			}
		}
		[NonSerialized]
		private Type cachedType;

		public Type TypeAsList {
			get {
				var type = Type;
				if (Type == null) {
					return null;
				}
				return typeof(List<>).MakeGenericType(type);
			}
		}

		public virtual string TypeName() {
			var type = Type;
			if (type == null) {
				return "(none)";
			}
			if (list) {
				return $"List<{Type.GetType(typeName, true).Name}>";
			}
			return $"{type.Name}";
		}

		public void ApplyChanges() {
			cachedType = null;
			OnChanged?.Invoke();
		}

	}
}