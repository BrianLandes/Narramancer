using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Narramancer;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// What the migration tool can <b>see</b>.
/// <para>
/// Adding the component is the easy half. The hard half is finding the objects that need one, because a scene
/// object a verb writes into carries no marker of its own — what makes it saved is a
/// <see cref="VariableAssignment"/> pointing at it from somewhere else in the scene. The first version of the
/// search reflected over each component's own fields and so found only assignments held directly; the
/// Character Equipment sample holds its five three levels down and the tool reported the scene clean while
/// leaving every label broken. These tests are the nesting shapes that exist in the shipped scenes.
/// </para>
/// </summary>
public class SaveableMigrationTests {

	private readonly List<GameObject> created = new List<GameObject>();

	[TearDown]
	public void TearDown() {
		foreach (var gameObject in created) {
			if (gameObject != null) {
				Object.DestroyImmediate(gameObject);
			}
		}
		created.Clear();
	}

	private GameObject NewObject(string name) {
		var gameObject = new GameObject(name);
		created.Add(gameObject);
		return gameObject;
	}

	private static void SetPrivateField(object target, string field, object value) {
		var info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.IsNotNull(info, $"field '{field}' not found on {target.GetType().Name} — the shape changed");
		info.SetValue(target, value);
	}

	/// <summary>
	/// The regression. <see cref="NarramancerScene"/> keeps its assignments behind a list wrapper
	/// (<c>variables</c> → <c>.list</c> → a port → <c>.assignment</c>), which is three hops further than a
	/// one-level field scan reaches. This is the exact shape of all five references in the Character Equipment
	/// sample.
	/// </summary>
	[Test]
	public void AnAssignmentNestedInsideAListWrapperIsFound() {
		var target = NewObject("Character Title");
		var host = NewObject("Narramancer Scene").AddComponent<NarramancerScene>();
		var variable = new NarramancerPortWithAssignment { Name = "Character Title" };
		variable.Assignment.objectValue = target;
		host.Variables.Add(variable);

		var found = SaveableMigration.AssignedObjectsOn(host).ToList();

		CollectionAssert.Contains(found, target,
			"a one-level field scan stops at the list wrapper and reports the scene clean");
	}

	/// <summary>
	/// The shape the old scan did handle, kept so the rewrite is not a trade of one blind spot for another.
	/// </summary>
	[Test]
	public void AnAssignmentInAPlainListIsStillFound() {
		var target = NewObject("Weapon UI");
		var host = NewObject("Run Verb").AddComponent<RunActionVerbMonoBehaviour>();
		SetPrivateField(host, "assignments", new List<VariableAssignment> {
			new VariableAssignment { objectValue = target },
		});

		CollectionAssert.Contains(SaveableMigration.AssignedObjectsOn(host).ToList(), target);
	}

	[Test]
	public void EveryAssignmentIsFoundNotJustTheFirst() {
		var host = NewObject("Narramancer Scene").AddComponent<NarramancerScene>();
		var targets = new[] { NewObject("Title"), NewObject("Description"), NewObject("Weapon UI") };
		foreach (var target in targets) {
			var variable = new NarramancerPortWithAssignment();
			variable.Assignment.objectValue = target;
			host.Variables.Add(variable);
		}

		CollectionAssert.AreEquivalent(targets, SaveableMigration.AssignedObjectsOn(host).ToList());
	}

	/// <summary>
	/// An unassigned variable is the common case in an authored scene, and it must not be reported as an
	/// object needing an id — the tool would be adding components to nothing.
	/// </summary>
	[Test]
	public void AnEmptyAssignmentIsNotReported() {
		var host = NewObject("Narramancer Scene").AddComponent<NarramancerScene>();
		host.Variables.Add(new NarramancerPortWithAssignment());

		Assert.IsEmpty(SaveableMigration.AssignedObjectsOn(host).ToList());
	}

	/// <summary>
	/// The search walks every serialized property of every component in the scene, so it has to match on an
	/// assignment and not merely on something holding an object reference — most components hold several.
	/// </summary>
	[Test]
	public void AComponentThatMerelyReferencesAnObjectYieldsNothing() {
		var host = NewObject("Ordinary").AddComponent<PlainReferenceHolder>();
		host.other = NewObject("Something Else");

		Assert.IsEmpty(SaveableMigration.AssignedObjectsOn(host).ToList(),
			"only an assignment means 'this object is saved'; an ordinary reference does not");
	}

	/// <summary>A stand-in for a game's own component: an object reference that is not an assignment.</summary>
	private class PlainReferenceHolder : MonoBehaviour {
		public GameObject other = null;
	}
}
