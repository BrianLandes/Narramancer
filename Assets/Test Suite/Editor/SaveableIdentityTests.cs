using System.Collections.Generic;
using System.Linq;
using Narramancer;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Scene-object identity. A save keyed to an object has to still find that object after the scene has been
/// edited, and before this it did not.
/// <para>
/// The key was <c>"{field} {transform.FullPath()}[{componentIndex}]"</c>. Renaming the object, reparenting it,
/// reordering its components, or two siblings sharing a name all changed it — and a key that no longer matches
/// does not error, it reads back as the default. The save loaded and the object was quietly wrong. Every test
/// here is a thing that used to break it.
/// </para>
/// </summary>
public class SaveableIdentityTests {

	private readonly List<GameObject> created = new List<GameObject>();

	[SetUp]
	public void SetUp() {
		Saveable.ClearClaims();
	}

	[TearDown]
	public void TearDown() {
		foreach (var gameObject in created) {
			if (gameObject != null) {
				Object.DestroyImmediate(gameObject);
			}
		}
		created.Clear();
		Saveable.ClearClaims();
	}

	/// <summary>
	/// A GameObject with a Saveable that has been given its id.
	/// <para>
	/// <c>EnsureGuid</c> is called explicitly because none of Unity's minting callbacks run here:
	/// <c>AddComponent</c> does not fire <c>Reset</c>, and EditMode has no <c>Awake</c>. That is exactly why
	/// minting is a public, idempotent method rather than only a lifecycle callback.
	/// </para>
	/// </summary>
	private Saveable Object_(string name) {
		var gameObject = new GameObject(name);
		created.Add(gameObject);
		var saveable = gameObject.AddComponent<Saveable>();
		saveable.EnsureGuid();
		return saveable;
	}

	private static string KeyOf(SerializableMonoBehaviour component, string field) => component.Key(field);

	private SerializeTransform Saver(Saveable saveable) {
		return saveable.gameObject.AddComponent<SerializeTransform>();
	}

	#region The guid itself

	[Test]
	public void AddingASaveableMintsAGuid() {
		Assert.IsTrue(Object_("Hero").HasGuid);
	}

	[Test]
	public void TwoObjectsGetDifferentGuids() {
		Assert.AreNotEqual(Object_("A").Guid, Object_("B").Guid);
	}

	/// <summary>
	/// Copy-pasting a GameObject copies the serialized guid, so the copy arrives claiming an id that already
	/// belongs to something else. Whoever arrives second re-mints, so the original keeps the id its existing
	/// saves refer to.
	/// </summary>
	[Test]
	public void DuplicatingAnObjectGivesTheCopyANewGuid() {
		var original = Object_("Hero");
		var originalGuid = original.Guid;

		var copy = Object.Instantiate(original.gameObject);
		created.Add(copy);
		var copySaveable = copy.GetComponent<Saveable>();
		// Instantiate does not run Awake outside play mode, so this stands in for it. In the editor the paste
		// fires OnValidate; in play, Awake; either way the same call.
		copySaveable.EnsureGuid();

		Assert.AreNotEqual(originalGuid, copySaveable.Guid, "the copy must not answer to the original's id");
		Assert.AreEqual(originalGuid, original.Guid, "and the original must keep the id its saves refer to");
	}

	#endregion

	#region The things that used to break a save

	[Test]
	public void RenamingTheObjectDoesNotChangeItsKey() {
		var saveable = Object_("Hero");
		var saver = Saver(saveable);
		var before = KeyOf(saver, "position");

		saveable.gameObject.name = "Hero (renamed)";

		Assert.AreEqual(before, KeyOf(saver, "position"));
	}

	[Test]
	public void ReparentingTheObjectDoesNotChangeItsKey() {
		var parent = new GameObject("Parent");
		created.Add(parent);
		var saveable = Object_("Hero");
		var saver = Saver(saveable);
		var before = KeyOf(saver, "position");

		saveable.transform.SetParent(parent.transform);

		Assert.AreEqual(before, KeyOf(saver, "position"));
	}

	[Test]
	public void ReorderingComponentsDoesNotChangeTheKey() {
		// The old key ended in [componentIndex]. Note that *adding* a component does not exercise this —
		// a new component goes below, leaving existing indices alone — so this actually moves one, the way
		// the inspector's Move Up does.
		var saveable = Object_("Hero");
		var saver = Saver(saveable);
		var before = KeyOf(saver, "position");

		Assume.That(UnityEditorInternal.ComponentUtility.MoveComponentUp(saver), Is.True,
			"precondition: the component actually moved");

		Assert.AreEqual(before, KeyOf(saver, "position"));
	}

	[Test]
	public void TwoSiblingsWithTheSameNameDoNotShareAKey() {
		// Identical name, identical parent, identical component layout: under the old path key these two were
		// indistinguishable, so one silently overwrote the other's saved state.
		var parent = new GameObject("Parent");
		created.Add(parent);

		var first = Object_("Item");
		var second = Object_("Item");
		first.transform.SetParent(parent.transform);
		second.transform.SetParent(parent.transform);

		Assert.AreNotEqual(KeyOf(Saver(first), "position"), KeyOf(Saver(second), "position"));
	}

	[Test]
	public void TheKeyCarriesTheGuidAndTheComponentType() {
		var saveable = Object_("Hero");
		var saver = Saver(saveable);

		var key = KeyOf(saver, "position");

		StringAssert.Contains(saveable.Guid, key);
		StringAssert.Contains(nameof(SerializeTransform), key,
			"two different savers on one object must not collide");
		StringAssert.DoesNotContain("Hero", key, "the name must not be part of the identity any more");
	}

	[Test]
	public void TwoDifferentSaversOnOneObjectDoNotCollide() {
		var saveable = Object_("Hero");
		var transformSaver = saveable.gameObject.AddComponent<SerializeTransform>();
		var activeSaver = saveable.gameObject.AddComponent<SerializeActive>();

		Assert.AreNotEqual(KeyOf(transformSaver, "state"), KeyOf(activeSaver, "state"));
	}

	#endregion

	#region Missing identity

	/// <summary>
	/// A saver with no Saveable cannot be given a durable key. It says so and falls back to the old path key,
	/// so the rest of the save still round-trips rather than every orphan colliding on one empty guid.
	/// </summary>
	[Test]
	public void ASaverWithNoSaveableReportsItAndStillProducesAUniqueKey() {
		var first = new GameObject("Orphan A");
		var second = new GameObject("Orphan B");
		created.Add(first);
		created.Add(second);
		var firstSaver = first.AddComponent<SerializeTransform>();
		var secondSaver = second.AddComponent<SerializeTransform>();

		UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error,
			new System.Text.RegularExpressions.Regex("Orphan A.*Saveable"));
		var firstKey = KeyOf(firstSaver, "position");
		UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error,
			new System.Text.RegularExpressions.Regex("Orphan B.*Saveable"));
		var secondKey = KeyOf(secondSaver, "position");

		Assert.AreNotEqual(firstKey, secondKey);
	}

	#endregion
}
