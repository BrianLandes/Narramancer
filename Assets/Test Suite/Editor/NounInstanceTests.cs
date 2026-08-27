using System.Collections.Generic;
using System.Linq;
using Narramancer;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Domain behaviour for <see cref="NounInstance"/> — the adjectives (properties, stats, relationships) that
/// hang off a noun. These run entirely in memory; only the property paths touch
/// <see cref="NarramancerSingleton"/>, which resolves from Assets/Resources in EditMode.
/// </summary>
public class NounInstanceTests {

	private readonly List<Object> createdAssets = new List<Object>();

	[TearDown]
	public void TearDown() {
		foreach (var asset in createdAssets) {
			if (asset != null) {
				Object.DestroyImmediate(asset);
			}
		}
		createdAssets.Clear();
	}

	private T Asset<T>(string name) where T : ScriptableObject {
		var asset = ScriptableObject.CreateInstance<T>();
		asset.name = name;
		createdAssets.Add(asset);
		return asset;
	}

	private NounInstance NewInstance(string name = "Wren") {
		return new StoryInstance().CreateInstance(Asset<NounScriptableObject>(name));
	}

	#region Identity

	[Test]
	public void EachInstanceGetsADistinctUid() {
		var story = new StoryInstance();
		var a = story.CreateInstance(Asset<NounScriptableObject>("Wren"));
		var b = story.CreateInstance(Asset<NounScriptableObject>("Brook"));

		Assert.AreNotEqual(a.UID.ToString(), b.UID.ToString());
	}

	[Test]
	public void GenerateNewUidChangesIt() {
		var instance = NewInstance();
		var before = instance.UID.ToString();

		instance.GenerateNewUID();

		Assert.AreNotEqual(before, instance.UID.ToString());
	}

	/// <summary>
	/// <see cref="NounUID"/> is a class, so this would be reference equality were it not for the implicit
	/// string conversion. Relationship matching depends on it comparing by *value* — after a reload the two
	/// sides are different objects with equal values.
	/// </summary>
	[Test]
	public void UidComparesByValueNotReference() {
		var original = new NounUID();
		var copy = new NounUID(original.ToString());

		Assert.AreNotSame(original, copy);
		Assert.IsTrue(original == copy, "two NounUIDs with the same value must compare equal");
		Assert.IsTrue(original.Equals(copy));
		Assert.AreEqual(original.GetHashCode(), copy.GetHashCode(), "equal values must hash equally");
	}

	[Test]
	public void UidComparesEqualToItsOwnStringForm() {
		// The implicit string conversion makes these interchangeable at call sites, so == and Equals must agree.
		var uid = new NounUID();
		var asString = uid.ToString();

		Assert.IsTrue(uid == asString);
		Assert.IsTrue(uid.Equals(asString));
	}

	[Test]
	public void UidHandlesNullsWithoutThrowing() {
		NounUID nothing = null;
		var something = new NounUID();

		Assert.IsTrue(nothing == null);
		Assert.IsFalse(something == null);
		Assert.IsTrue(something != null);
		Assert.IsFalse(something.Equals(null));
	}

	/// <summary>
	/// Regression for the reason <see cref="NounUID"/> needed a value-equality operator at all: this lookup
	/// compares with <c>==</c>, so under reference equality it returned null for any caller that passed a UID
	/// string rather than the original object.
	/// </summary>
	[Test]
	public void RelationshipMatchingWorksAcrossDistinctUidObjects() {
		var story = new StoryInstance();
		var wren = story.CreateInstance(Asset<NounScriptableObject>("Wren"));
		var brook = story.CreateInstance(Asset<NounScriptableObject>("Brook"));
		var sibling = Asset<RelationshipScriptableObject>("Sibling");
		wren.AddRelationship(sibling, brook, SourceOrDestination.Source);

		// A UID rebuilt from its string form — a different object with the same value, which is exactly what
		// a serializer that doesn't preserve shared references would hand back.
		var rebuiltWrenUid = new NounUID(wren.UID.ToString());
		Assert.AreNotSame(wren.UID, rebuiltWrenUid);

		Assert.IsTrue(wren.Relationships[0].SourceUID == rebuiltWrenUid,
			"relationship matching must not depend on UID object identity");
	}

	#endregion

	#region Properties

	[Test]
	public void AddAndRemoveProperty() {
		var instance = NewInstance();
		var cursed = Asset<PropertyScriptableObject>("Cursed");

		Assert.IsFalse(instance.HasProperty(cursed));

		instance.AddProperty(cursed);
		Assert.IsTrue(instance.HasProperty(cursed));
		Assert.AreEqual(1, instance.GetAllProperties().Count);

		instance.RemoveProperty(cursed);
		Assert.IsFalse(instance.HasProperty(cursed));
		Assert.IsEmpty(instance.GetAllProperties());
	}

	[Test]
	public void AddingTheSamePropertyTwiceIsIdempotent() {
		var instance = NewInstance();
		var cursed = Asset<PropertyScriptableObject>("Cursed");

		instance.AddProperty(cursed);
		instance.AddProperty(cursed);

		Assert.AreEqual(1, instance.GetAllProperties().Count, "a property should not be duplicated");
	}

	[Test]
	public void PropertiesAreIndependentPerInstance() {
		var story = new StoryInstance();
		var wren = story.CreateInstance(Asset<NounScriptableObject>("Wren"));
		var brook = story.CreateInstance(Asset<NounScriptableObject>("Brook"));
		var cursed = Asset<PropertyScriptableObject>("Cursed");

		wren.AddProperty(cursed);

		Assert.IsTrue(wren.HasProperty(cursed));
		Assert.IsFalse(brook.HasProperty(cursed), "adding to one noun must not affect another");
	}

	#endregion

	#region Stats

	[Test]
	public void AddAndReadStat() {
		var instance = NewInstance();
		var health = Asset<StatScriptableObject>("Health");

		Assert.IsFalse(instance.HasStat(health));

		var stat = instance.InstantiateAndAddStat(health);
		stat.Value = 12f;

		Assert.IsTrue(instance.HasStat(health));
		Assert.AreEqual(12f, instance.GetStatInstance(health).Value);
		Assert.AreEqual(1, instance.GetAllStats().Count);
	}

	[Test]
	public void StatValueWithNoClampsStoresExactly() {
		// With neither min nor max activated the setter must not alter the value. This also guards the
		// clamp fix from the Tier A batch, which previously read the parameter instead of the stored field.
		var instance = NewInstance();
		var stat = instance.InstantiateAndAddStat(Asset<StatScriptableObject>("Health"));

		stat.Value = -7.5f;
		Assert.AreEqual(-7.5f, stat.Value);

		stat.Value = 1234.5f;
		Assert.AreEqual(1234.5f, stat.Value);
	}

	#endregion

	#region Relationships

	[Test]
	public void AddRelationshipRegistersOnBothParticipants() {
		var story = new StoryInstance();
		var wren = story.CreateInstance(Asset<NounScriptableObject>("Wren"));
		var brook = story.CreateInstance(Asset<NounScriptableObject>("Brook"));
		var sibling = Asset<RelationshipScriptableObject>("Sibling");

		wren.AddRelationship(sibling, brook, SourceOrDestination.Source);

		Assert.AreEqual(1, wren.Relationships.Count);
		Assert.AreEqual(1, brook.Relationships.Count);
		Assert.AreSame(wren.Relationships[0], brook.Relationships[0],
			"both sides share one RelationshipInstance");
	}

	[Test]
	public void RelationshipDirectionIsRecorded() {
		var story = new StoryInstance();
		var wren = story.CreateInstance(Asset<NounScriptableObject>("Wren"));
		var brook = story.CreateInstance(Asset<NounScriptableObject>("Brook"));
		var parentOf = Asset<RelationshipScriptableObject>("ParentOf");

		wren.AddRelationship(parentOf, brook, SourceOrDestination.Source);
		var relationship = wren.Relationships[0];

		Assert.IsTrue(relationship.InvolvesSource(wren));
		Assert.IsTrue(relationship.InvolvesDestination(brook));
		Assert.IsFalse(relationship.InvolvesSource(brook), "direction must not be symmetric");
		Assert.IsFalse(relationship.InvolvesDestination(wren));
	}

	[Test]
	public void AddingTheSameRelationshipTwiceIsIdempotent() {
		var story = new StoryInstance();
		var wren = story.CreateInstance(Asset<NounScriptableObject>("Wren"));
		var brook = story.CreateInstance(Asset<NounScriptableObject>("Brook"));
		var sibling = Asset<RelationshipScriptableObject>("Sibling");

		wren.AddRelationship(sibling, brook, SourceOrDestination.Source);
		wren.AddRelationship(sibling, brook, SourceOrDestination.Source);

		Assert.AreEqual(1, wren.Relationships.Count);
		Assert.AreEqual(1, brook.Relationships.Count);
	}

	[Test]
	public void HasRelationshipRespectsDirection() {
		var story = new StoryInstance();
		var wren = story.CreateInstance(Asset<NounScriptableObject>("Wren"));
		var brook = story.CreateInstance(Asset<NounScriptableObject>("Brook"));
		var parentOf = Asset<RelationshipScriptableObject>("ParentOf");

		wren.AddRelationship(parentOf, brook, SourceOrDestination.Source);

		Assert.IsTrue(wren.HasRelationship(parentOf, brook, RelationshipRequirement.Source));
		Assert.IsFalse(brook.HasRelationship(parentOf, wren, RelationshipRequirement.Source),
			"brook is the destination, not the source");
		Assert.IsTrue(brook.HasRelationship(parentOf, wren, RelationshipRequirement.Destination));
	}

	[Test]
	public void GetRelationshipsWithFindsTheOtherParticipant() {
		var story = new StoryInstance();
		var wren = story.CreateInstance(Asset<NounScriptableObject>("Wren"));
		var brook = story.CreateInstance(Asset<NounScriptableObject>("Brook"));
		var stranger = story.CreateInstance(Asset<NounScriptableObject>("Stranger"));
		var sibling = Asset<RelationshipScriptableObject>("Sibling");

		wren.AddRelationship(sibling, brook, SourceOrDestination.Source);

		Assert.AreEqual(1, wren.GetRelationshipsWith(sibling).Count);
		Assert.IsEmpty(stranger.GetRelationshipsWith(sibling), "an uninvolved noun has no relationships");
	}

	#endregion
}
