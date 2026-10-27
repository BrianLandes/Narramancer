using System.Collections.Generic;
using Narramancer;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// The scene components that carry a story across a load: <see cref="NarramancerScene"/>,
/// <see cref="RunActionVerbMonoBehaviour"/> and <see cref="SerializableSpawner"/>.
/// <para>
/// Each test saves from one set of objects and loads into a <em>different</em> set that answers to the same
/// guids — which is what a load is: the scene is reloaded from disk, so nothing that did the saving is still
/// alive. Loading back into the same objects would pass for the wrong reason.
/// </para>
/// <para>
/// None of Unity's lifecycle callbacks run here (EditMode has no <c>Awake</c> or <c>Start</c>), so identity is
/// minted with <c>EnsureGuid</c> and the singleton's registry is bypassed by calling <c>Serialize</c> /
/// <c>Deserialize</c> directly, as <c>PrepareStoryForSave</c> and <c>LoadStory</c> would.
/// </para>
/// </summary>
public class SceneComponentSaveTests {

	private readonly List<Object> created = new List<Object>();

	[SetUp]
	public void SetUp() {
		Saveable.ClearClaims();
	}

	[TearDown]
	public void TearDown() {
		foreach (var obj in created) {
			if (obj != null) {
				Object.DestroyImmediate(obj);
			}
		}
		created.Clear();
		Saveable.ClearClaims();
		Narramancer.Serialization.RuntimeAssetRegistry.Clear();
	}

	private GameObject Object_(string name, string guid = null) {
		var gameObject = new GameObject(name);
		created.Add(gameObject);
		var saveable = gameObject.AddComponent<Saveable>();
		if (guid != null) {
			saveable.AssignGuid(guid);
		}
		else {
			saveable.EnsureGuid();
		}
		return gameObject;
	}

	private static StoryInstance NewStoryForSave() {
		var story = new StoryInstance();
		story.SaveTable = new Blackboard();
		return story;
	}

	private static StoryInstance RoundTrip(StoryInstance story) {
		return SaveLoadUtilities.DeserializePayload<StoryInstance>(SaveLoadUtilities.SerializePayload(story));
	}

	/// <summary>
	/// Stands in for the scene being torn down by a load: the saving objects are gone, and so is their claim on
	/// their guids. Without this the loading objects would be seen as duplicate pastes and re-minted.
	/// </summary>
	private void UnloadScene() {
		foreach (var obj in created) {
			if (obj != null) {
				Object.DestroyImmediate(obj);
			}
		}
		created.Clear();
		Saveable.ClearClaims();
	}

	#region NarramancerScene

	/// <summary>
	/// <see cref="NarramancerScene"/> keeps its run-on-start runners in a list, saved through the save table,
	/// while the story keeps the same runners in its runner table. After a load those must still be the
	/// <em>same</em> objects: the table is what gets ticked, so a list holding copies would show the scene a
	/// runner that never moves again.
	/// </summary>
	[Test]
	public void ASceneHoldsTheStorysOwnRunnersAfterALoad() {
		var scene = Object_("Narramancer Scene").AddComponent<NarramancerScene>();
		var guid = scene.GetComponent<Saveable>().Guid;
		var runner = new NodeRunner { name = "Intro" };
		scene.NodeRunners.Add(runner);

		var story = NewStoryForSave();
		story.NodeRunners["Intro"] = runner;
		scene.Serialize(story);

		var restored = RoundTrip(story);
		UnloadScene();

		var loaded = Object_("Narramancer Scene", guid).AddComponent<NarramancerScene>();
		loaded.Deserialize(restored);

		Assert.AreEqual(1, loaded.NodeRunners.Count, "the scene's runner list should survive the load");
		Assert.AreSame(restored.NodeRunners["Intro"], loaded.NodeRunners[0],
			"the scene must hold the runner the story ticks, not a copy of it");
	}

	#endregion

	#region RunActionVerbMonoBehaviour

	/// <summary>
	/// The runner is filed in the story's runner table under the component's guid key, so it is found again
	/// by identity — renaming the object between save and load must not lose it.
	/// </summary>
	[Test]
	public void ARunVerbComponentFindsItsRunnerAfterALoadEvenIfRenamed() {
		var component = Object_("Door").AddComponent<RunActionVerbMonoBehaviour>();
		var guid = component.GetComponent<Saveable>().Guid;
		var runnerKey = component.Key("runner");
		var runner = new NodeRunner { name = runnerKey };
		typeof(RunActionVerbMonoBehaviour)
			.GetField("runner", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
			.SetValue(component, runner);

		var story = NewStoryForSave();
		story.NodeRunners[runnerKey] = runner;
		component.Serialize(story);

		var restored = RoundTrip(story);
		UnloadScene();

		var loaded = Object_("Renamed Door", guid).AddComponent<RunActionVerbMonoBehaviour>();
		loaded.Deserialize(restored);

		Assert.IsNotNull(loaded.Runner, "the runner should be found by guid, whatever the object is called");
		Assert.AreSame(restored.NodeRunners[runnerKey], loaded.Runner);
	}

	#endregion

	#region SerializableSpawner

	private GameObject Template(string name) {
		var template = Object_(name);
		var child = new GameObject("Child");
		child.transform.SetParent(template.transform);
		child.AddComponent<Saveable>().EnsureGuid();
		child.AddComponent<SerializeTransform>();
		return template;
	}

	private SerializableSpawner Spawner(string guid, GameObject template) {
		var spawner = Object_("Spawner", guid).AddComponent<SerializableSpawner>();
		var serializedObject = new SerializedObject(spawner);
		serializedObject.FindProperty(SerializableSpawner.PrefabFieldName).objectReferenceValue = template;
		serializedObject.ApplyModifiedPropertiesWithoutUndo();
		return spawner;
	}

	/// <summary>
	/// Spawns once, simulating the <c>Awake</c> that would notice the clone copied the template's guids and
	/// mint its own.
	/// </summary>
	private static GameObject SpawnWithFreshIds(SerializableSpawner spawner) {
		var spawn = spawner.Spawn();
		foreach (var saveable in spawn.GetComponentsInChildren<Saveable>(true)) {
			saveable.EnsureGuid();
		}
		return spawn;
	}

	/// <summary>
	/// The riskiest part of the guid switch. A spawned object's state is keyed by guids minted at spawn time;
	/// on load the spawner makes a <em>new</em> object, which would mint new ones and find nothing. The spawner
	/// has to put the saved guids back before the spawn reads its state — and if it does not, nothing errors,
	/// the object just comes back at its prefab defaults.
	/// </summary>
	[Test]
	public void ARespawnedObjectGetsItsGuidsAndItsStateBack() {
		var spawner = Spawner(null, Template("Template"));
		var spawnerGuid = spawner.GetComponent<Saveable>().Guid;

		var spawn = SpawnWithFreshIds(spawner);
		var child = spawn.transform.Find("Child");
		var childGuid = child.GetComponent<Saveable>().Guid;
		Assert.AreNotEqual(spawnerGuid, childGuid, "precondition: the spawn has an id of its own");
		child.localPosition = new Vector3(3, 4, 5);

		var story = NewStoryForSave();
		spawner.Serialize(story);
		child.GetComponent<SerializeTransform>().Serialize(story);

		var restored = RoundTrip(story);
		UnloadScene();

		var loadedSpawner = Spawner(spawnerGuid, Template("Template"));
		loadedSpawner.Deserialize(restored);

		var respawned = loadedSpawner.GetAllGameObjects();
		Assert.AreEqual(1, respawned.Count, "the spawner should re-spawn what it had");
		var respawnedChild = respawned[0].transform.Find("Child");
		Assert.AreEqual(childGuid, respawnedChild.GetComponent<Saveable>().Guid,
			"the replacement must answer to the guid its saved state is keyed by");
		Assert.AreEqual(new Vector3(3, 4, 5), respawnedChild.localPosition,
			"the replacement's state should be read back, not left at the prefab's defaults");
	}

	/// <summary>
	/// Two spawns from one prefab must come back as two distinct identities, each with its own state — the
	/// failure here would be both reading one record, or the second overwriting the first.
	/// </summary>
	[Test]
	public void TwoRespawnedObjectsKeepTheirOwnState() {
		var spawner = Spawner(null, Template("Template"));
		var spawnerGuid = spawner.GetComponent<Saveable>().Guid;

		var first = SpawnWithFreshIds(spawner).transform.Find("Child");
		var second = SpawnWithFreshIds(spawner).transform.Find("Child");
		first.localPosition = Vector3.left;
		second.localPosition = Vector3.right;

		var story = NewStoryForSave();
		spawner.Serialize(story);
		first.GetComponent<SerializeTransform>().Serialize(story);
		second.GetComponent<SerializeTransform>().Serialize(story);

		var restored = RoundTrip(story);
		UnloadScene();

		var loadedSpawner = Spawner(spawnerGuid, Template("Template"));
		loadedSpawner.Deserialize(restored);

		var respawned = loadedSpawner.GetAllGameObjects();
		Assert.AreEqual(2, respawned.Count);
		Assert.AreEqual(Vector3.left, respawned[0].transform.Find("Child").localPosition);
		Assert.AreEqual(Vector3.right, respawned[1].transform.Find("Child").localPosition);
	}

	#endregion

	#region A component the save never saw

	/// <summary>
	/// The patch case: an object added to the scene after the save was written. There is nothing saved for
	/// it, and it used to throw — inside <c>LoadStory</c>'s loop, which abandoned the load for every
	/// component after it.
	/// </summary>
	[Test]
	public void ATransformTheSaveNeverSawKeepsItsSceneState() {
		var restored = RoundTrip(NewStoryForSave());

		var added = Object_("Added In A Patch").AddComponent<SerializeTransform>();
		added.transform.position = new Vector3(1, 2, 3);

		Assert.DoesNotThrow(() => added.Deserialize(restored));
		Assert.AreEqual(new Vector3(1, 2, 3), added.transform.position);
	}

	/// <summary>A missing bool reads back as false, so this used to hide the object.</summary>
	[Test]
	public void AnObjectTheSaveNeverSawStaysActive() {
		var restored = RoundTrip(NewStoryForSave());

		var added = Object_("Added In A Patch").AddComponent<SerializeActive>();
		added.Deserialize(restored);

		Assert.IsTrue(added.gameObject.activeSelf, "nothing was saved for it, so nothing should change");
	}

	/// <summary>
	/// The other side of the two tests above: a value that was saved as its type's default is a real value,
	/// and has to be restored. Telling those apart is the whole point of the saved marker.
	/// </summary>
	[Test]
	public void AStateSavedAsTheDefaultIsStillRestored() {
		var saver = Object_("Hidden Door").AddComponent<SerializeActive>();
		var guid = saver.GetComponent<Saveable>().Guid;
		saver.gameObject.SetActive(false);

		var story = NewStoryForSave();
		saver.Serialize(story);
		var restored = RoundTrip(story);
		UnloadScene();

		var loaded = Object_("Hidden Door", guid).AddComponent<SerializeActive>();
		Assert.IsTrue(loaded.gameObject.activeSelf, "precondition: the scene builds it active");
		loaded.Deserialize(restored);

		Assert.IsFalse(loaded.gameObject.activeSelf, "it was saved hidden, and false is a value");
	}

	#endregion

	#region Choice printers

	private static readonly System.Reflection.BindingFlags Private =
		System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;

	private ChoicePrinter ShowingChoices(string name) {
		var printer = Object_(name).AddComponent<ChoicePrinter>();
		typeof(ChoicePrinter).GetField("showingChoices", Private).SetValue(printer, true);
		return printer;
	}

	/// <summary>
	/// The printers filed their choices under the literal key <c>"choices"</c>, so two of them — or a
	/// <see cref="ChoicePrinter"/> and a <see cref="TextAndChoicePrinter"/>, which used the same literal —
	/// overwrote each other, and both loaded whichever was saved last.
	/// </summary>
	[Test]
	public void TwoChoicePrintersKeepTheirOwnChoices() {
		var first = ShowingChoices("First Printer");
		var second = ShowingChoices("Second Printer");
		var firstChoices = typeof(ChoicePrinter).GetField("choices", Private).GetValue(first);
		var secondChoices = typeof(ChoicePrinter).GetField("choices", Private).GetValue(second);

		var story = NewStoryForSave();
		first.Serialize(story);
		second.Serialize(story);

		Assert.AreSame(firstChoices, story.SaveTable.GetObject(first.Key("choices")));
		Assert.AreSame(secondChoices, story.SaveTable.GetObject(second.Key("choices")));
		Assert.IsFalse(story.SaveTable.Contains<object>("choices"), "nothing should be filed under the bare literal");
	}

	#endregion

	#region Noun creation order

	private static readonly System.Reflection.FieldInfo StoryField =
		typeof(NarramancerSingleton).GetField("storyInstance", Private);

	/// <summary>
	/// Anything that asked for an object's noun before <see cref="CreateNounForGameObject"/>'s <c>Start</c>
	/// ran used to get null. Now asking creates it — once, however many ask.
	/// </summary>
	[Test]
	public void AskingForANounBeforeStartCreatesItExactlyOnce() {
		var previousStory = StoryField.GetValue(NarramancerSingleton.Instance);
		var story = new StoryInstance();
		StoryField.SetValue(NarramancerSingleton.Instance, story);
		try {
			var gameObject = Object_("Hero");
			var creator = gameObject.AddComponent<CreateNounForGameObject>();
			var reference = gameObject.AddComponent<SerializeNounInstanceReference>();

			var instance = reference.GetInstance();

			Assert.IsNotNull(instance, "asking before Start should create the noun, not answer null");
			Assert.AreSame(gameObject, instance.GameObject);
			Assert.AreSame(instance, creator.EnsureInstance(), "a second ask must not create a second noun");
			Assert.AreEqual(1, story.Instances.Count);
		}
		finally {
			StoryField.SetValue(NarramancerSingleton.Instance, previousStory);
		}
	}

	#endregion
}
