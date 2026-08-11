using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Narramancer;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class BlackboardTests {

	enum MyEnum {
		Red,
		Blue,
		Yellow
	}

	#region Generic Set/Get

	[Test]
	public void SetInt() {
		var blackboard = new Blackboard();
		blackboard.Set("value", 2319);

		var value = blackboard.Get<int>("value");
		Assert.AreEqual(value, 2319);
	}

	[Test]
	public void SetEnum() {
		var blackboard = new Blackboard();
		blackboard.Set("value", MyEnum.Yellow);

		var value = blackboard.Get<MyEnum>("value");
		Assert.AreEqual(value, MyEnum.Yellow);
	}

	[Test]
	public void SetFloat() {
		var blackboard = new Blackboard();
		blackboard.Set("value", 1.5f);

		Assert.AreEqual(1.5f, blackboard.Get<float>("value"));
	}

	[Test]
	public void SetBool() {
		var blackboard = new Blackboard();
		blackboard.Set("value", true);

		Assert.IsTrue(blackboard.Get<bool>("value"));
	}

	[Test]
	public void SetString() {
		var blackboard = new Blackboard();
		blackboard.Set("value", "narramancer");

		Assert.AreEqual("narramancer", blackboard.Get<string>("value"));
	}

	[Test]
	public void SetOverwritesExistingValue() {
		var blackboard = new Blackboard();
		blackboard.Set("value", 1);
		blackboard.Set("value", 2);

		Assert.AreEqual(2, blackboard.Get<int>("value"));
	}

	[Test]
	public void SameKeyInDifferentTypesDoesNotCollide() {
		// Each type is backed by its own dictionary, so one key can legitimately hold a value per type.
		var blackboard = new Blackboard();
		blackboard.Set("value", 7);
		blackboard.Set("value", "seven");
		blackboard.Set("value", true);

		Assert.AreEqual(7, blackboard.Get<int>("value"));
		Assert.AreEqual("seven", blackboard.Get<string>("value"));
		Assert.IsTrue(blackboard.Get<bool>("value"));
	}

	#endregion

	#region Missing keys return defaults rather than throwing

	[Test]
	public void GetMissingKeyReturnsDefault() {
		var blackboard = new Blackboard();

		Assert.AreEqual(0, blackboard.Get<int>("absent"));
		Assert.AreEqual(0f, blackboard.Get<float>("absent"));
		Assert.IsFalse(blackboard.Get<bool>("absent"));
		Assert.AreEqual(string.Empty, blackboard.Get<string>("absent"));
	}

	[Test]
	public void GetWithExplicitDefaultReturnsThatDefault() {
		var blackboard = new Blackboard();

		Assert.AreEqual(42, blackboard.GetInt("absent", 42));
		Assert.AreEqual(2.5f, blackboard.GetFloat("absent", 2.5f));
		Assert.IsTrue(blackboard.GetBool("absent", true));
		Assert.AreEqual("fallback", blackboard.GetString("absent", "fallback"));
	}

	#endregion

	#region TryGet

	[Test]
	public void TryGetReportsPresence() {
		var blackboard = new Blackboard();
		blackboard.SetInt("present", 5);
		blackboard.SetFloat("presentFloat", 1.25f);

		Assert.IsTrue(blackboard.TryGetInt("present", out var intValue));
		Assert.AreEqual(5, intValue);
		Assert.IsFalse(blackboard.TryGetInt("absent", out _));

		Assert.IsTrue(blackboard.TryGetFloat("presentFloat", out var floatValue));
		Assert.AreEqual(1.25f, floatValue);
		Assert.IsFalse(blackboard.TryGetFloat("absent", out _));
	}

	[Test]
	public void TryGetIntOnMissingKeyEmitsTheSuppliedDefault() {
		var blackboard = new Blackboard();

		Assert.IsFalse(blackboard.TryGetInt("absent", out var value, 99));
		Assert.AreEqual(99, value);
	}

	#endregion

	#region Remove

	[Test]
	public void RemoveDropsTheValue() {
		var blackboard = new Blackboard();
		blackboard.Set("value", 2319);
		blackboard.Remove<int>("value");

		Assert.IsFalse(blackboard.TryGetInt("value", out _));
		Assert.AreEqual(0, blackboard.Get<int>("value"));
	}

	[Test]
	public void RemoveOnlyAffectsTheMatchingType() {
		var blackboard = new Blackboard();
		blackboard.Set("value", 7);
		blackboard.Set("value", "seven");

		blackboard.Remove<int>("value");

		Assert.AreEqual(0, blackboard.Get<int>("value"));
		Assert.AreEqual("seven", blackboard.Get<string>("value"), "removing the int should not disturb the string");
	}

	[Test]
	public void RemoveMissingKeyIsHarmless() {
		var blackboard = new Blackboard();

		Assert.DoesNotThrow(() => blackboard.Remove<int>("absent"));
		Assert.DoesNotThrow(() => blackboard.Remove<string>("absent"));
	}

	[Test]
	public void GetAndRemoveReturnsTheValueThenClearsIt() {
		var blackboard = new Blackboard();
		blackboard.Set("value", 2319);

		Assert.AreEqual(2319, blackboard.GetAndRemove<int>("value"));
		Assert.IsFalse(blackboard.TryGetInt("value", out _), "the key should be gone after GetAndRemove");
	}

	[Test]
	public void ClearEmptiesEveryType() {
		var blackboard = new Blackboard();
		blackboard.Set("i", 1);
		blackboard.Set("s", "text");
		blackboard.Set("b", true);
		blackboard.Set("f", 1f);

		blackboard.Clear();

		Assert.AreEqual(0, blackboard.Get<int>("i"));
		Assert.AreEqual(string.Empty, blackboard.Get<string>("s"));
		Assert.IsFalse(blackboard.Get<bool>("b"));
		Assert.AreEqual(0f, blackboard.Get<float>("f"));
	}

	#endregion

	#region Int convenience helpers

	[Test]
	public void IncrementAndDecrementInt() {
		var blackboard = new Blackboard();
		blackboard.SetInt("count", 5);

		blackboard.IncrementInt("count");
		Assert.AreEqual(6, blackboard.GetInt("count"));

		blackboard.DecrementInt("count");
		blackboard.DecrementInt("count");
		Assert.AreEqual(4, blackboard.GetInt("count"));
	}

	[Test]
	public void IncrementOnMissingKeyStartsFromZero() {
		var blackboard = new Blackboard();
		blackboard.IncrementInt("fresh");

		Assert.AreEqual(1, blackboard.GetInt("fresh"));
	}

	[Test]
	public void IntKeysListsOnlyIntKeys() {
		var blackboard = new Blackboard();
		blackboard.SetInt("a", 1);
		blackboard.SetInt("b", 2);
		blackboard.SetString("c", "not an int");

		var keys = blackboard.IntKeys().ToList();

		CollectionAssert.AreEquivalent(new[] { "a", "b" }, keys);
	}

	[Test]
	public void IntKeysOnEmptyBlackboardIsEmptyNotNull() {
		var blackboard = new Blackboard();

		Assert.IsNotNull(blackboard.IntKeys());
		Assert.IsEmpty(blackboard.IntKeys());
	}

	#endregion

	#region Copy

	[Test]
	public void CopyCarriesValuesAcross() {
		var blackboard = new Blackboard();
		blackboard.Set("i", 2319);
		blackboard.Set("s", "text");

		var copy = blackboard.Copy();

		Assert.AreEqual(2319, copy.Get<int>("i"));
		Assert.AreEqual("text", copy.Get<string>("s"));
	}

	[Test]
	public void CopyIsIndependentOfTheOriginal() {
		var blackboard = new Blackboard();
		blackboard.Set("i", 1);

		var copy = blackboard.Copy();
		copy.Set("i", 2);

		Assert.AreEqual(1, blackboard.Get<int>("i"), "mutating the copy must not write through to the original");
		Assert.AreEqual(2, copy.Get<int>("i"));
	}

	#endregion

	#region Serialization

	[Test]
	public void Serialize() {
		var blackboard = new Blackboard();
		blackboard.Set("int", 2319);
		blackboard.Set("enum", MyEnum.Yellow);

		var json = JsonUtility.ToJson(blackboard);

		blackboard = JsonUtility.FromJson<Blackboard>(json);

		var intValue = blackboard.Get<int>("int");
		Assert.AreEqual(intValue, 2319);
		var enumValue = blackboard.Get<MyEnum>("enum");
		Assert.AreEqual(enumValue, MyEnum.Yellow);
	}

	[Test]
	public void SerializeRoundTripsEveryPrimitiveType() {
		var blackboard = new Blackboard();
		blackboard.Set("i", 2319);
		blackboard.Set("f", 1.5f);
		blackboard.Set("b", true);
		blackboard.Set("s", "narramancer");

		var restored = JsonUtility.FromJson<Blackboard>(JsonUtility.ToJson(blackboard));

		Assert.AreEqual(2319, restored.Get<int>("i"));
		Assert.AreEqual(1.5f, restored.Get<float>("f"));
		Assert.IsTrue(restored.Get<bool>("b"));
		Assert.AreEqual("narramancer", restored.Get<string>("s"));
	}

	#endregion
}
