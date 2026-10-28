using System;
using System.Collections.Generic;
using Narramancer;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Resolving a <see cref="SerializedProperty"/> back to the object it draws. Every custom drawer leans on this,
/// and the version it replaced only understood a top-level field or a single <c>list.Array.data[i].field</c>
/// hop — so a drawer used one level deeper, on a field inside a serializable class, silently got null.
/// </summary>
public class EditorDrawerUtilitiesTests {

	[Serializable]
	public class Deeper {
		public string label = "deeper";
	}

	[Serializable]
	public class Inner {
		public string label = "inner";
		public Deeper deeper = new Deeper();
	}

	public class Holder : ScriptableObject {
		public Inner inner = new Inner();
		public List<Inner> inners = new List<Inner> { new Inner(), new Inner() };
	}

	public class BaseHolder : ScriptableObject {
		[SerializeField]
		private Inner basePrivate = new Inner();
		public Inner BasePrivate => basePrivate;
	}

	public class DerivedHolder : BaseHolder { }

	private readonly List<UnityEngine.Object> created = new List<UnityEngine.Object>();

	[TearDown]
	public void TearDown() {
		foreach (var obj in created) {
			UnityEngine.Object.DestroyImmediate(obj);
		}
		created.Clear();
	}

	private T New<T>() where T : ScriptableObject {
		var instance = ScriptableObject.CreateInstance<T>();
		created.Add(instance);
		return instance;
	}

	private static SerializedProperty Property(UnityEngine.Object target, string path) {
		var property = new SerializedObject(target).FindProperty(path);
		Assert.IsNotNull(property, $"precondition: Unity serializes '{path}'");
		return property;
	}

	[Test]
	public void ATopLevelFieldResolvesToItsValue() {
		var holder = New<Holder>();
		var property = Property(holder, "inner");

		Assert.AreSame(holder.inner, property.GetTargetObject<Inner>());
		Assert.AreEqual(nameof(Holder.inner), property.GetFieldInfo().Name);
		Assert.AreSame(holder, property.GetTargetObjectParent());
	}

	[Test]
	public void AFieldInsideANestedClassResolvesToItsValue() {
		var holder = New<Holder>();
		var property = Property(holder, "inner.deeper");

		Assert.AreSame(holder.inner.deeper, property.GetTargetObject<Deeper>());
		Assert.AreEqual(nameof(Inner.deeper), property.GetFieldInfo().Name);
		Assert.AreSame(holder.inner, property.GetTargetObjectParent());
		Assert.AreEqual(typeof(Inner), property.GetParentType(), "attributes are looked up on the declaring type");
	}

	[Test]
	public void AListElementResolvesToTheElementNotTheList() {
		var holder = New<Holder>();
		var property = Property(holder, "inners.Array.data[1]");

		Assert.AreSame(holder.inners[1], property.GetTargetObject<Inner>());
		Assert.AreEqual(typeof(Inner), property.GetPropertyTargetType(), "the element type, not List<Inner>");
		Assert.AreEqual(nameof(Holder.inners), property.GetFieldInfo().Name);
	}

	[Test]
	public void AFieldInsideAListElementResolvesToThatElementsValue() {
		var holder = New<Holder>();
		var property = Property(holder, "inners.Array.data[1].deeper");

		Assert.AreSame(holder.inners[1].deeper, property.GetTargetObject<Deeper>());
		Assert.AreSame(holder.inners[1], property.GetTargetObjectParent());
	}

	[Test]
	public void APrivateFieldDeclaredInABaseClassResolves() {
		var holder = New<DerivedHolder>();
		var property = Property(holder, "basePrivate.deeper");

		Assert.AreSame(holder.BasePrivate.deeper, property.GetTargetObject<Deeper>());
	}

	[Test]
	public void TheFirstFieldOfATypeIsFoundIncludingBaseClassPrivates() {
		Assert.AreEqual(nameof(Holder.inner), New<Holder>().GetFirstFieldWithType<Inner>());
		Assert.AreEqual("basePrivate", New<DerivedHolder>().GetFirstFieldWithType<Inner>());
		Assert.IsNull(New<Holder>().GetFirstFieldWithType<Texture2D>());
	}
}
