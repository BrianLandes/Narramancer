using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Narramancer;
using Narramancer.Serialization;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Regression cover for the nine Tier A bugs fixed on 2026-08-12 with no tests at all, plus the
/// <see cref="InequalityNode"/> operator fix that landed with them.
/// <para>
/// Only the pure-logic ones are worth a harness. The two editor-UI fixes (port style, an icon) are left
/// uncovered on purpose — a test for either would assert more about Unity's inspector than about this code.
/// </para>
/// </summary>
public class TierARegressionTests {

	private readonly List<Object> created = new List<Object>();
	private ActionVerb graph;

	[SetUp]
	public void SetUp() {
		graph = ScriptableObject.CreateInstance<ActionVerb>();
		graph.name = "Test Verb";
		created.Add(graph);
		RuntimeAssetRegistry.Clear();
	}

	[TearDown]
	public void TearDown() {
		RuntimeAssetRegistry.Clear();
		foreach (var node in graph.nodes.ToList()) {
			if (node != null) {
				Object.DestroyImmediate(node);
			}
		}
		foreach (var obj in created) {
			if (obj != null) {
				Object.DestroyImmediate(obj);
			}
		}
		created.Clear();
	}

	private T Asset<T>(string name) where T : ScriptableObject {
		var asset = ScriptableObject.CreateInstance<T>();
		asset.name = name;
		created.Add(asset);
		return asset;
	}

	/// <summary>Sets a protected/private field — node inputs are not public.</summary>
	private static void SetField(object target, string field, object value) {
		var info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
		Assert.IsNotNull(info, $"field '{field}' not found on {target.GetType().Name} — the shape changed");
		info.SetValue(target, value);
	}

	#region InequalityNode — the operators were wrong

	private bool Compare(float a, InequalityNode.Comparison comparison, float b) {
		var node = graph.AddNode<InequalityNode>();
		SetField(node, "a", a);
		SetField(node, "b", b);
		SetField(node, "comparison", comparison);
		return (bool)node.GetValue(new Blackboard(), node.GetOutputPort("result"));
	}

	/// <summary>
	/// The guarantee that made this fix free. Unity serializes an enum field by its <b>integer</b> value, so
	/// an authored graph stores the index. The two misnamed members kept their indices and the strict
	/// operators were appended, so every existing comparison still evaluates exactly as it did — it just
	/// finally has the right label. Reordering this enum would silently change authored graphs, which is why
	/// the indices are asserted rather than assumed.
	/// </summary>
	[Test]
	public void TheComparisonEnumIndicesAreFrozen() {
		Assert.AreEqual(0, (int)InequalityNode.Comparison.ApproximatelyEqualTo);
		Assert.AreEqual(1, (int)InequalityNode.Comparison.GreaterThanOrEqualTo,
			"index 1 was 'GreaterThan' behaving as >=, so it must stay >=");
		Assert.AreEqual(2, (int)InequalityNode.Comparison.LessThanOrEqualTo,
			"index 2 was 'LessThan' behaving as <=, so it must stay <=");
		Assert.AreEqual(3, (int)InequalityNode.Comparison.GreaterThan, "the strict operators are appended");
		Assert.AreEqual(4, (int)InequalityNode.Comparison.LessThan);
	}

	[Test]
	public void GreaterThanIsStrict() {
		Assert.IsTrue(Compare(2f, InequalityNode.Comparison.GreaterThan, 1f));
		Assert.IsFalse(Compare(1f, InequalityNode.Comparison.GreaterThan, 1f), "strictly greater excludes equal");
		Assert.IsFalse(Compare(0f, InequalityNode.Comparison.GreaterThan, 1f));
	}

	[Test]
	public void LessThanIsStrict() {
		Assert.IsTrue(Compare(0f, InequalityNode.Comparison.LessThan, 1f));
		Assert.IsFalse(Compare(1f, InequalityNode.Comparison.LessThan, 1f), "strictly less excludes equal");
		Assert.IsFalse(Compare(2f, InequalityNode.Comparison.LessThan, 1f));
	}

	[Test]
	public void GreaterThanOrEqualToIncludesEqual() {
		Assert.IsTrue(Compare(2f, InequalityNode.Comparison.GreaterThanOrEqualTo, 1f));
		Assert.IsTrue(Compare(1f, InequalityNode.Comparison.GreaterThanOrEqualTo, 1f));
		Assert.IsFalse(Compare(0f, InequalityNode.Comparison.GreaterThanOrEqualTo, 1f));
	}

	[Test]
	public void LessThanOrEqualToIncludesEqual() {
		Assert.IsTrue(Compare(0f, InequalityNode.Comparison.LessThanOrEqualTo, 1f));
		Assert.IsTrue(Compare(1f, InequalityNode.Comparison.LessThanOrEqualTo, 1f));
		Assert.IsFalse(Compare(2f, InequalityNode.Comparison.LessThanOrEqualTo, 1f));
	}

	[Test]
	public void ApproximatelyEqualToToleratesFloatingPointDrift() {
		Assert.IsTrue(Compare(0.1f + 0.2f, InequalityNode.Comparison.ApproximatelyEqualTo, 0.3f),
			"exact equality would fail here, which is the reason this operator exists");
		Assert.IsFalse(Compare(1f, InequalityNode.Comparison.ApproximatelyEqualTo, 2f));
	}

	#endregion

	#region StatInstance — a stat with both a min and a max only honoured the max

	private StatInstance Stat(float min, float max) {
		var noun = Asset<NounScriptableObject>("Wren");
		var statAsset = Asset<StatScriptableObject>("Health");
		statAsset.MinValue.activated = true;
		statAsset.MinValue.value = min;
		statAsset.MaxValue.activated = true;
		statAsset.MaxValue.value = max;
		return new StatInstance(statAsset, new StoryInstance().CreateInstance(noun));
	}

	/// <summary>
	/// The bug: the second clamp read the incoming parameter rather than the already-clamped field, so it
	/// recomputed from the unclamped input and discarded the first clamp. A stat with both bounds only ever
	/// honoured the max.
	/// </summary>
	[Test]
	public void AStatWithBothBoundsHonoursTheMinimum() {
		var stat = Stat(min: 5f, max: 10f);

		stat.Value = 0f;

		Assert.AreEqual(5f, stat.Value, "the minimum was being discarded by the maximum clamp");
	}

	[Test]
	public void AStatWithBothBoundsHonoursTheMaximum() {
		var stat = Stat(min: 5f, max: 10f);

		stat.Value = 20f;

		Assert.AreEqual(10f, stat.Value);
	}

	[Test]
	public void AStatInsideItsBoundsIsLeftAlone() {
		var stat = Stat(min: 5f, max: 10f);

		stat.Value = 7f;

		Assert.AreEqual(7f, stat.Value);
	}

	#endregion

	#region NounInstancesQuery — it is a dictionary key, so Equals and GetHashCode must agree

	private static NounInstancesQuery Query(PropertyScriptableObject[] mustHave, PropertyScriptableObject[] mustNotHave) {
		return new NounInstancesQuery { mustHaveProperties = mustHave, mustNotHaveProperties = mustNotHave };
	}

	/// <summary>
	/// Either array can be left null by a query that does not constrain on it, which used to throw in
	/// <c>Equals</c>. Null and empty both mean "no constraint", so they have to compare equal.
	/// </summary>
	[Test]
	public void ANullConstraintEqualsAnEmptyOne() {
		Assert.AreEqual(Query(null, null), Query(new PropertyScriptableObject[0], new PropertyScriptableObject[0]));
	}

	[Test]
	public void ANullConstraintHashesTheSameAsAnEmptyOne() {
		// Equality without hash agreement is worse than either being wrong alone: the query is used as a
		// dictionary key, so two equal keys landing in different buckets means a silent cache miss.
		Assert.AreEqual(Query(null, null).GetHashCode(),
			Query(new PropertyScriptableObject[0], new PropertyScriptableObject[0]).GetHashCode());
	}

	[Test]
	public void ComparingAgainstNullDoesNotThrow() {
		Assert.DoesNotThrow(() => Query(null, null).Equals(null));
		Assert.IsFalse(Query(null, null).Equals(null));
	}

	[Test]
	public void QueriesWithDifferentConstraintsAreNotEqual() {
		var property = Asset<PropertyScriptableObject>("Wounded");

		Assert.AreNotEqual(Query(new[] { property }, null), Query(null, null));
		Assert.AreNotEqual(Query(new[] { property }, null), Query(null, new[] { property }),
			"must-have and must-not-have are opposite constraints, not interchangeable");
	}

	[Test]
	public void AQueryWorksAsADictionaryKey() {
		var property = Asset<PropertyScriptableObject>("Wounded");
		var cache = new Dictionary<NounInstancesQuery, string>();

		cache[Query(new[] { property }, null)] = "wounded";

		Assert.IsTrue(cache.TryGetValue(Query(new[] { property }, null), out var found),
			"an equal query must find the entry it stored");
		Assert.AreEqual("wounded", found);
	}

	#endregion

	#region AssemblyUtilities — the nested-namespace type name regex

	/// <summary>
	/// The regex used to stop at the first dot, so any type in a nested namespace failed to match at all.
	/// The quantifier is greedy now, so the last dotted segment is the class name.
	/// </summary>
	[Test]
	public void ATypeInANestedNamespaceResolvesFromItsQualifiedName() {
		var type = typeof(Narramancer.Serialization.NarramancerSerializer);
		Assume.That(type.Namespace.Count(character => character == '.'), Is.GreaterThan(0),
			"precondition: this type really is in a nested namespace");

		Assert.AreEqual(type, AssemblyUtilities.GetType(type.AssemblyQualifiedName));
	}

	[Test]
	public void TheClassNameIsTakenFromTheLastSegment() {
		var type = typeof(Narramancer.Serialization.NarramancerSerializer);

		Assert.AreEqual(type.Name, AssemblyUtilities.GetClassName(type.AssemblyQualifiedName));
	}

	[Test]
	public void ATopLevelNamespaceStillResolves() {
		Assert.AreEqual(typeof(StoryInstance), AssemblyUtilities.GetType(typeof(StoryInstance).AssemblyQualifiedName));
	}

	#endregion

	#region AddRelationshipNode — the null guard

	/// <summary>
	/// With nothing wired into its inputs the node used to dereference null. It now reports the problem and
	/// carries on, which matters because a graph author sees the message and a crash stops the story.
	/// </summary>
	[Test]
	public void AddingARelationshipWithNothingWiredReportsRatherThanThrows() {
		var node = graph.AddNode<AddRelationshipNode>();
		var runner = new NodeRunner { name = "main" };

		UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error,
			new System.Text.RegularExpressions.Regex("null"));

		Assert.DoesNotThrow(() => node.Run(runner));
	}

	#endregion
}
