using System.Collections;
using System.Collections.Generic;
using Narramancer;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class BlackboardTests {

	[Test]
	public void SetInt() {
		var blackboard = new Blackboard();
		blackboard.Set("value", 2319);

		var value = blackboard.Get<int>("value");
		Assert.AreEqual(value, 2319);
	}

	enum MyEnum {
		Red,
		Blue,
		Yellow
	}

	[Test]
	public void SetEnum() {
		var blackboard = new Blackboard();
		blackboard.Set("value", MyEnum.Yellow);

		var value = blackboard.Get<MyEnum>("value");
		Assert.AreEqual(value, MyEnum.Yellow);
	}

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

}
