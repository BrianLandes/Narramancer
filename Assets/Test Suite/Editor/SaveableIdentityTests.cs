using System.Collections.Generic;
using System.Linq;
using Narramancer;
using Narramancer.Serialization;
using NUnit.Framework;
using UnityEditor;
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

	private const string TestFolder = "Assets/Temp Saveable Tests";

	private readonly List<GameObject> created = new List<GameObject>();

	[SetUp]
	public void SetUp() {
		Saveable.ClearClaims();
		if (!AssetDatabase.IsValidFolder(TestFolder)) {
			AssetDatabase.CreateFolder("Assets", System.IO.Path.GetFileName(TestFolder));
		}
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
		if (AssetDatabase.IsValidFolder(TestFolder)) {
			AssetDatabase.DeleteAsset(TestFolder);
		}
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

	/// <summary>Any <see cref="SerializableMonoBehaviour"/> will do; this one has no setup of its own.</summary>
	private SerializeNounInstanceReference Saver(Saveable saveable) {
		return saveable.gameObject.AddComponent<SerializeNounInstanceReference>();
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
		var after = KeyOf(saver, "position");

		// MoveComponentUp is undoable, and the test runner reverts undo after a run — which rebuilt this object,
		// already destroyed, into whatever scene was open. Forget the undo step so there is nothing to replay.
		Undo.ClearUndo(saver);
		Undo.ClearUndo(saveable.gameObject);

		Assert.AreEqual(before, after);
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
		StringAssert.Contains(nameof(SerializeNounInstanceReference), key,
			"two different savers on one object must not collide");
		StringAssert.DoesNotContain("Hero", key, "the name must not be part of the identity any more");
	}

	[Test]
	public void TwoDifferentSaversOnOneObjectDoNotCollide() {
		var saveable = Object_("Hero");
		var nounSaver = saveable.gameObject.AddComponent<SerializeNounInstanceReference>();
		var verbSaver = saveable.gameObject.AddComponent<RunActionVerbMonoBehaviour>();

		Assert.AreNotEqual(KeyOf(nounSaver, "state"), KeyOf(verbSaver, "state"));
	}

	#endregion

	#region Prefabs

	/// <summary>
	/// A prefab <b>asset</b> must never carry an id, or every instance made from it answers to the same one
	/// and they all overwrite each other's saved state.
	/// <para>
	/// It can pick one up by accident: saving a scene object as a prefab copies whatever guid it had. So the
	/// asset is actively cleared rather than merely skipped.
	/// </para>
	/// </summary>
	[Test]
	public void APrefabAssetCarriesNoGuid() {
		var source = Object_("Hero");
		Assume.That(source.HasGuid, "precondition: the scene object has an id to accidentally copy");

		var path = AssetDatabase.GenerateUniqueAssetPath($"{TestFolder}/Hero.prefab");
		var prefab = PrefabUtility.SaveAsPrefabAsset(source.gameObject, path);
		var asset = prefab.GetComponent<Saveable>();

		Assert.IsEmpty(asset.EnsureGuid(), "a prefab asset is a template, not a thing in the world");
		Assert.IsFalse(asset.HasGuid);
	}

	[Test]
	public void TwoInstancesOfOnePrefabGetDifferentGuids() {
		var source = Object_("Hero");
		var path = AssetDatabase.GenerateUniqueAssetPath($"{TestFolder}/Hero.prefab");
		var prefab = PrefabUtility.SaveAsPrefabAsset(source.gameObject, path);
		prefab.GetComponent<Saveable>().EnsureGuid();

		var first = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
		var second = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
		created.Add(first);
		created.Add(second);
		var firstGuid = first.GetComponent<Saveable>().EnsureGuid();
		var secondGuid = second.GetComponent<Saveable>().EnsureGuid();

		Assert.IsNotEmpty(firstGuid);
		Assert.AreNotEqual(firstGuid, secondGuid, "two instances of one prefab are two different objects");
	}

	#endregion

	#region Scene objects as saved references

	/// <summary>
	/// The bug this closes, in the shape it was reported. The Character Equipment sample keeps its UI
	/// <c>Text</c> objects in a blackboard; those references were dropped on save because a scene object had
	/// no id, so after a load <c>SetTextNode</c> found null and did nothing — labels at their scene defaults,
	/// nothing in the log.
	/// </summary>
	[Test]
	public void AComponentInABlackboardSurvivesASave() {
		var saveable = Object_("Equipment Label");
		var label = saveable.gameObject.AddComponent<UnityEngine.UI.Text>();
		var story = new StoryInstance();
		story.Blackboard.Set("label", label);

		var serializer = new NarramancerSerializer(new UnityReferenceResolver(null));
		var restored = serializer.Deserialize<StoryInstance>(serializer.Serialize(story));

		Assert.AreSame(label, restored.Blackboard.Get<UnityEngine.UI.Text>("label"),
			"a scene component in a blackboard must come back as the same component");
	}

	[Test]
	public void AGameObjectInABlackboardSurvivesASave() {
		var saveable = Object_("Hero");
		var story = new StoryInstance();
		story.Blackboard.Set("hero", saveable.gameObject);

		var serializer = new NarramancerSerializer(new UnityReferenceResolver(null));
		var restored = serializer.Deserialize<StoryInstance>(serializer.Serialize(story));

		Assert.AreSame(saveable.gameObject, restored.Blackboard.Get<GameObject>("hero"));
	}

	[Test]
	public void ASceneReferenceIsWrittenAsASceneId() {
		var saveable = Object_("Hero");
		var story = new StoryInstance();
		story.Blackboard.Set("hero", saveable.gameObject);

		var json = new NarramancerSerializer(new UnityReferenceResolver(null)).Serialize(story);

		StringAssert.Contains(SceneObjectIds.Prefix + saveable.Guid, json);
	}

	/// <summary>
	/// One object can hold several components, so the id carries the type too — otherwise restoring a
	/// reference to the Text would be as likely to hand back the Image.
	/// </summary>
	[Test]
	public void TwoComponentsOnOneObjectResolveSeparately() {
		// Text and CanvasGroup, not Text and Image: both of those derive from Graphic, which is
		// [DisallowMultipleComponent], so the second AddComponent returns null.
		var saveable = Object_("Panel");
		var text = saveable.gameObject.AddComponent<UnityEngine.UI.Text>();
		var group = saveable.gameObject.AddComponent<CanvasGroup>();
		var story = new StoryInstance();
		story.Blackboard.Set("text", text);
		story.Blackboard.Set("group", group);

		var serializer = new NarramancerSerializer(new UnityReferenceResolver(null));
		var restored = serializer.Deserialize<StoryInstance>(serializer.Serialize(story));

		Assert.AreSame(text, restored.Blackboard.Get<UnityEngine.UI.Text>("text"));
		Assert.AreSame(group, restored.Blackboard.Get<CanvasGroup>("group"));
	}

	/// <summary>
	/// Found in the 2026-10-27 WebGL build: a save pointing at a scene object refused to load because the object
	/// "could not be found". Claims are made in <c>Awake</c>, and Unity never calls <c>Awake</c> on an object
	/// that starts inactive, so a freshly loaded scene's hidden objects answered to nothing. Clearing the claims
	/// after saving stands in for that fresh load.
	/// </summary>
	[Test]
	public void AReferenceToAnObjectThatNeverWokeStillResolves() {
		var saveable = Object_("Hidden Panel");
		saveable.gameObject.SetActive(false);
		var story = new StoryInstance();
		story.Blackboard.Set("panel", saveable.gameObject);

		var serializer = new NarramancerSerializer(new UnityReferenceResolver(null));
		var json = serializer.Serialize(story);
		Saveable.ClearClaims();

		var restored = serializer.Deserialize<StoryInstance>(json);

		Assert.AreSame(saveable.gameObject, restored.Blackboard.Get<GameObject>("panel"),
			"an inactive object has never claimed its guid, and must still be found");
	}

	/// <summary>
	/// A scene object with no <see cref="Saveable"/> still cannot be named — and now says so, instead of the
	/// reference quietly disappearing.
	/// </summary>
	[Test]
	public void ASceneObjectWithNoSaveableIsReportedNotDroppedSilently() {
		var orphan = new GameObject("Orphan");
		created.Add(orphan);
		var story = new StoryInstance();
		story.Blackboard.Set("orphan", orphan);

		UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error,
			new System.Text.RegularExpressions.Regex("Orphan"));

		new NarramancerSerializer(new UnityReferenceResolver(null)).Serialize(story);
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
		var firstSaver = first.AddComponent<SerializeNounInstanceReference>();
		var secondSaver = second.AddComponent<SerializeNounInstanceReference>();

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
