using System.Collections.Generic;
using System.Linq;
using Narramancer;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// The regression net for the headline feature: a story survives a serialize/deserialize round trip.
/// <para>
/// These target <see cref="SaveLoadUtilities.SerializePayload{T}"/> / <c>DeserializePayload</c> rather than
/// <c>SerializeData</c>, because the latter also captures a screenshot for the thumbnail, which EditMode has no
/// frame to capture. The payload pair is the exact seam a serializer swap replaces, so this is also the layer
/// that has to keep passing when OdinSerializer goes away.
/// </para>
/// </summary>
public class SaveLoadRoundTripTests {

	private readonly List<Object> createdAssets = new List<Object>();

	[TearDown]
	public void TearDown() {
		Narramancer.Serialization.RuntimeAssetRegistry.Clear();
		foreach (var asset in createdAssets) {
			if (asset != null) {
				Object.DestroyImmediate(asset);
			}
		}
		createdAssets.Clear();
	}

	/// <summary>
	/// Creates a ScriptableObject that is cleaned up after the test, and registers it so a save can name it.
	/// <para>
	/// The registration is not ceremony. An object created with <c>CreateInstance</c> is not an asset and has
	/// no persistent identity, so the save system correctly refuses to write a reference to one. Odin papered
	/// over that by storing an <c>instanceID</c>, which is exactly the bug the GUID work removed — these tests
	/// were quietly relying on identity that never survived a rebuild.
	/// </para>
	/// </summary>
	private T Asset<T>(string name) where T : ScriptableObject {
		var asset = ScriptableObject.CreateInstance<T>();
		asset.name = name;
		createdAssets.Add(asset);
		Narramancer.Serialization.RuntimeAssetRegistry.Register($"test-{createdAssets.Count}-{name}", asset);
		return asset;
	}

	/// <summary>Round-trips through the payload seam, carrying the external object list across as the save
	/// file does.</summary>
	private static StoryInstance RoundTrip(StoryInstance story) {
		var payload = SaveLoadUtilities.SerializePayload(story);
		Assert.IsNotNull(payload, "serializing produced no payload");
		Assert.IsNotEmpty(payload, "serializing produced an empty payload");
		return SaveLoadUtilities.DeserializePayload<StoryInstance>(payload);
	}

	#region The graph survives at all

	[Test]
	public void EmptyStoryRoundTrips() {
		var restored = RoundTrip(new StoryInstance());

		Assert.IsNotNull(restored);
		Assert.IsNotNull(restored.Instances, "instances list should not come back null");
		Assert.IsEmpty(restored.Instances);
	}

	[Test]
	public void StoryBlackboardValuesSurvive() {
		var story = new StoryInstance();
		story.Blackboard.Set("chapter", 3);
		story.Blackboard.Set("hero", "Wren");
		story.Blackboard.Set("ratio", 0.5f);
		story.Blackboard.Set("flagged", true);

		var restored = RoundTrip(story);

		Assert.AreEqual(3, restored.Blackboard.Get<int>("chapter"));
		Assert.AreEqual("Wren", restored.Blackboard.Get<string>("hero"));
		Assert.AreEqual(0.5f, restored.Blackboard.Get<float>("ratio"));
		Assert.IsTrue(restored.Blackboard.Get<bool>("flagged"));
	}

	[Test]
	public void SceneIndexAndTextLogsSurvive() {
		var story = new StoryInstance();
		story.sceneIndex = 4;
		story.AddTextLog("the forest thickens");

		var restored = RoundTrip(story);

		Assert.AreEqual(4, restored.sceneIndex);
		CollectionAssert.Contains(restored.TextLogs, "the forest thickens");
	}

	#endregion

	#region Nouns

	[Test]
	public void NounInstanceSurvivesWithIdentity() {
		var noun = Asset<NounScriptableObject>("Wren");
		var story = new StoryInstance();
		var instance = story.CreateInstance(noun);
		instance.DisplayName = "Wren the Wanderer";
		var originalUid = instance.UID.ToString();

		var restored = RoundTrip(story);

		Assert.AreEqual(1, restored.Instances.Count);
		Assert.AreEqual("Wren the Wanderer", restored.Instances[0].DisplayName);
		Assert.AreEqual(originalUid, restored.Instances[0].UID.ToString(), "the UID must survive — relationships key off it");
	}

	[Test]
	public void NounInstanceBlackboardSurvives() {
		var noun = Asset<NounScriptableObject>("Wren");
		var story = new StoryInstance();
		story.CreateInstance(noun).Blackboard.Set("mood", "wary");

		var restored = RoundTrip(story);

		Assert.AreEqual("wary", restored.Instances[0].Blackboard.Get<string>("mood"));
	}

	/// <summary>
	/// The save types have no parameterless constructor — <c>NounInstance(IInstancable)</c> is the only one, and
	/// the same is true of every <c>AdjectiveInstance</c>. A serializer therefore cannot use
	/// <c>Activator.CreateInstance</c>; it has to construct without running a constructor. Pinning that here so
	/// the replacement serializer can't quietly regress it.
	/// </summary>
	[Test]
	public void TypesWithoutParameterlessConstructorsAreReconstructed() {
		Assert.IsNull(
			typeof(NounInstance).GetConstructor(System.Type.EmptyTypes),
			"precondition: NounInstance still has no parameterless constructor");

		var noun = Asset<NounScriptableObject>("Wren");
		var story = new StoryInstance();
		story.CreateInstance(noun);

		var restored = RoundTrip(story);

		Assert.AreEqual(1, restored.Instances.Count);
		Assert.IsNotNull(restored.Instances[0]);
	}

	#endregion

	#region Relationships — the flagship

	/// <summary>
	/// Relationships are stored on both participants and matched by <see cref="NounUID"/>, not by object
	/// reference. After a round trip every instance is a *different* object, so this only passes if identity
	/// really is carried by the UID value. This is the single most load-bearing assertion in the suite.
	/// </summary>
	[Test]
	public void RelationshipIntegrityIsPreservedByUidAcrossTheBoundary() {
		var wrenNoun = Asset<NounScriptableObject>("Wren");
		var brookNoun = Asset<NounScriptableObject>("Brook");
		var sibling = Asset<RelationshipScriptableObject>("Sibling");

		var story = new StoryInstance();
		var wren = story.CreateInstance(wrenNoun);
		var brook = story.CreateInstance(brookNoun);
		wren.AddRelationship(sibling, brook, SourceOrDestination.Source);

		// Precondition: the relationship is on both sides, sharing one instance.
		Assert.AreEqual(1, wren.Relationships.Count, "source should hold the relationship");
		Assert.AreEqual(1, brook.Relationships.Count, "destination should hold the relationship too");

		var restored = RoundTrip(story);

		var restoredWren = restored.Instances.First(i => i.UID.ToString() == wren.UID.ToString());
		var restoredBrook = restored.Instances.First(i => i.UID.ToString() == brook.UID.ToString());

		Assert.AreEqual(1, restoredWren.Relationships.Count);
		Assert.AreEqual(1, restoredBrook.Relationships.Count);

		var relationship = restoredWren.Relationships[0];
		Assert.IsTrue(relationship.InvolvesSource(restoredWren), "source UID should still resolve after reload");
		Assert.IsTrue(relationship.InvolvesDestination(restoredBrook), "destination UID should still resolve after reload");
		Assert.IsTrue(restoredWren.HasRelationship(sibling, restoredBrook, RelationshipRequirement.Source));
	}

	/// <summary>
	/// Both participants hold the *same* <see cref="RelationshipInstance"/>. A serializer without shared-
	/// reference support would write it twice and restore two independent copies, so mutating one side would
	/// silently stop affecting the other.
	/// </summary>
	[Test]
	public void SharedRelationshipInstanceStaysShared() {
		var wrenNoun = Asset<NounScriptableObject>("Wren");
		var brookNoun = Asset<NounScriptableObject>("Brook");
		var sibling = Asset<RelationshipScriptableObject>("Sibling");

		var story = new StoryInstance();
		var wren = story.CreateInstance(wrenNoun);
		var brook = story.CreateInstance(brookNoun);
		wren.AddRelationship(sibling, brook, SourceOrDestination.Source);
		Assert.AreSame(wren.Relationships[0], brook.Relationships[0], "precondition: one shared instance");

		var restored = RoundTrip(story);

		var restoredWren = restored.Instances.First(i => i.UID.ToString() == wren.UID.ToString());
		var restoredBrook = restored.Instances.First(i => i.UID.ToString() == brook.UID.ToString());

		Assert.AreSame(restoredWren.Relationships[0], restoredBrook.Relationships[0],
			"the two sides must still point at one object, or edits to one stop affecting the other");
	}

	#endregion

	#region The save-file envelope

	/// <summary>
	/// A pre-1.0 save was written by OdinSerializer. Both formats use <c>$id</c> and <c>$type</c>, so without
	/// the version stamp an old payload fails somewhere deep in the reader on a type name — true, but no help
	/// to anyone reading the log.
	/// </summary>
	[Test]
	public void APreOneDotZeroSaveIsRejectedByName() {
		var oldSave = "{\"title\":\"Tuesday\",\"data\":\"{}\",\"thumbnail\":\"\"}";

		var exception = Assert.Throws<Narramancer.Serialization.SerializationException>(
			() => SaveLoadUtilities.DeserializeData<StoryInstance>(oldSave));

		StringAssert.Contains("pre-1.0", exception.Message);
		StringAssert.Contains("Tuesday", exception.Message, "the message should name the save it refused");
	}

	[Test]
	public void ASaveFromANewerBuildIsRejected() {
		var futureSave = $"{{\"version\":{Narramancer.Serialization.NarramancerSerializer.CurrentSchemaVersion + 1},\"title\":\"Later\",\"data\":\"{{}}\",\"thumbnail\":\"\"}}";

		var exception = Assert.Throws<Narramancer.Serialization.SerializationException>(
			() => SaveLoadUtilities.DeserializeData<StoryInstance>(futureSave));

		StringAssert.Contains("newer version", exception.Message);
	}

	/// <summary>
	/// Format 1 has no saved markers and no scene index on the envelope, so it would load as "nothing was saved
	/// for anything" — every component at its scene defaults, with no error. Refuse it by name instead.
	/// </summary>
	[Test]
	public void AFormatOneSaveIsRejectedRatherThanLoadedAsEmpty() {
		var formatOne = "{\"version\":1,\"title\":\"Early\",\"data\":\"{}\",\"thumbnail\":\"\"}";

		var exception = Assert.Throws<Narramancer.Serialization.SerializationException>(
			() => SaveLoadUtilities.DeserializeData<StoryInstance>(formatOne));

		StringAssert.Contains("Early", exception.Message);
		StringAssert.Contains("pre-release", exception.Message);
	}

	/// <summary>
	/// Loading reopens the save's scene before reading the story, so an unreadable save has to be refused up
	/// front — while the player is still where they were — not after the scene has already been left.
	/// </summary>
	[Test]
	public void LoadingAnUnreadableSaveFailsBeforeLeavingTheScene() {
		var formatOne = SaveLoadUtilities.DeserializeWrapper(
			"{\"version\":1,\"title\":\"Early\",\"data\":\"{}\",\"thumbnail\":\"\",\"sceneIndex\":0}");

		Assert.Throws<Narramancer.Serialization.SerializationException>(
			() => NarramancerSingleton.Instance.LoadStory(formatOne),
			"the refusal must happen synchronously, before any scene load is queued");
	}

	/// <summary>An envelope written before the scene index existed reads as "no scene", not as scene 0.</summary>
	[Test]
	public void AMissingSceneIndexReadsAsNone() {
		var wrapper = SaveLoadUtilities.DeserializeWrapper("{\"version\":2,\"title\":\"t\",\"data\":\"{}\",\"thumbnail\":\"\"}");

		Assert.AreEqual(-1, wrapper.sceneIndex);
	}

	#endregion

	#region Unity asset references

	/// <summary>
	/// An asset reference leaves the payload as an id handle rather than being inlined, and comes back as the
	/// same object. Rewritten at the serializer swap: this used to assert the reference appeared in the
	/// wrapper's external object list, which stored <c>instanceID</c>s — the thing the swap removed.
	/// <para>
	/// Passing here does not prove references survive a rebuild; only the patch test in a real build does.
	/// What it does prove is that the id in the payload is not session-local.
	/// </para>
	/// </summary>
	[Test]
	public void AssetReferencesAreWrittenAsHandlesAndResolveBack() {
		var noun = Asset<NounScriptableObject>("Wren");
		var story = new StoryInstance();
		story.CreateInstance(noun);

		var payload = SaveLoadUtilities.SerializePayload(story);

		StringAssert.Contains(Narramancer.Serialization.NarramancerSerializer.AssetKey, payload,
			"the noun should be referenced by handle, not inlined into the save");
		StringAssert.DoesNotContain(noun.GetInstanceID().ToString(), payload,
			"a session-local instanceID must never reach a save file");

		var restored = SaveLoadUtilities.DeserializePayload<StoryInstance>(payload);
		Assert.AreSame(noun, restored.Instances[0].Noun);
	}

	#endregion
}
