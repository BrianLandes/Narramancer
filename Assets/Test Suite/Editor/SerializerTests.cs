using System;
using System.Collections.Generic;
using Narramancer;
using Narramancer.Serialization;
using Narramancer.Serialization.Json;
using NUnit.Framework;
using UnityEngine;
using SerializationException = Narramancer.Serialization.SerializationException;

/// <summary>
/// The regression net for the replacement serializer (docs/ODIN_REMOVAL_PLAN.md, phase 2).
/// <para>
/// These test the serializer in isolation, against types declared here rather than against the story
/// graph, so a failure names one behaviour. The story-level round trip stays in
/// <see cref="SaveLoadRoundTripTests"/> and is what has to keep passing when the swap happens.
/// </para>
/// <para>
/// The field-policy tests are the load-bearing ones. v1 marks nearly all of its save state as
/// <c>[SerializeField] private</c>, and a policy that skipped those would still produce a payload that
/// parses — the failure would be silent and total.
/// </para>
/// </summary>
public class SerializerTests {

	private NarramancerSerializer serializer;
	private readonly List<UnityEngine.Object> createdAssets = new List<UnityEngine.Object>();

	[SetUp]
	public void SetUp() {
		serializer = new NarramancerSerializer();
	}

	[TearDown]
	public void TearDown() {
		foreach (var asset in createdAssets) {
			if (asset != null) {
				UnityEngine.Object.DestroyImmediate(asset);
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

	private T RoundTrip<T>(T value) {
		return serializer.Deserialize<T>(serializer.Serialize(value));
	}

	// --- test types ---

	private class Simple {
		public int PublicField;
		[SerializeField] private string privateSerialized;
		[NonSerialized] public string PublicButExcluded;
		private string privateAndUnmarked;

		public string PrivateSerialized { get => privateSerialized; set => privateSerialized = value; }
		public string PrivateAndUnmarked { get => privateAndUnmarked; set => privateAndUnmarked = value; }
	}

	private class Base {
		[SerializeField] private int baseValue;
		public int BaseValue { get => baseValue; set => baseValue = value; }
	}

	private class Derived : Base {
		[SerializeField] private int derivedValue;
		public int DerivedValue { get => derivedValue; set => derivedValue = value; }
	}

	private abstract class Shape {
		public string Name;
	}

	private class Circle : Shape {
		public float Radius;
	}

	private class Holder {
		public Shape Shape;
		public object Anything;
		public Simple First;
		public Simple Second;
	}

	private class Ring {
		public string Name;
		public Ring Next;
	}

	/// <summary>Stands in for the v1 save types, none of which declares a parameterless constructor.</summary>
	private class NoDefaultConstructor {
		public int Value;
		public NoDefaultConstructor(int value) {
			Value = value;
		}
	}

	private class Collections {
		public List<int> Numbers;
		public string[] Words;
		public Dictionary<string, Shape> Shapes;
		public StringIntDictionary Counts;
	}

	private class WithVector {
		public Vector3 Position;
	}

	private class WithType {
		public Type Value;
	}

	private class WithAsset {
		public NounScriptableObject Noun;
	}

	private enum Mood { Calm, Furious }

	private class WithEnum {
		public Mood Mood;
	}

	private class Scalar<T> {
		public T Value;
	}

	// --- field policy ---

	[Test]
	public void PrivateFieldsMarkedSerializeFieldAreSaved() {
		var original = new Simple { PublicField = 7, PrivateSerialized = "kept" };

		var result = RoundTrip(original);

		Assert.AreEqual(7, result.PublicField);
		Assert.AreEqual("kept", result.PrivateSerialized, "a [SerializeField] private field is where v1 keeps nearly all of its save state");
	}

	[Test]
	public void NonSerializedFieldsAreSkippedEvenWhenPublic() {
		var original = new Simple { PublicButExcluded = "transient" };

		var result = RoundTrip(original);

		Assert.IsNull(result.PublicButExcluded, "[NonSerialized] is load-bearing: Blackboard.gameObjects relies on it");
	}

	[Test]
	public void UnmarkedPrivateFieldsAreSkipped() {
		var original = new Simple { PrivateAndUnmarked = "incidental" };

		var result = RoundTrip(original);

		Assert.IsNull(result.PrivateAndUnmarked);
	}

	[Test]
	public void PrivateFieldsOnBaseClassesAreSaved() {
		var original = new Derived { BaseValue = 1, DerivedValue = 2 };

		var result = RoundTrip(original);

		Assert.AreEqual(1, result.BaseValue, "the field policy has to walk the whole inheritance chain");
		Assert.AreEqual(2, result.DerivedValue);
	}

	/// <summary>
	/// Inverted when M3 landed. Closures used to be serialized, with a carve-out that pulled in their private
	/// captured fields — and that was the bug: the ordinal in <c>&lt;&gt;c__DisplayClass22_0</c> moves when the
	/// source file is edited, so a save stopped resolving its own continuations after an unrelated change.
	/// <para>
	/// It refuses them now rather than merely lacking the carve-out. Without one a closure's fields are
	/// neither public nor <c>[SerializeField]</c>, so it would serialize as an <b>empty object</b> and lose
	/// its captured state silently — which is how this class of bug returns.
	/// </para>
	/// </summary>
	[Test]
	public void ACompilerGeneratedClosureIsRefusedRatherThanSilentlyEmptied() {
		var captured = new Simple { PublicField = 42 };
		Action action = () => captured.PublicField++;
		var closure = action.Target;
		Assume.That(closure, Is.Not.Null, "precondition: the lambda captures a local, so it has a display class");

		var exception = Assert.Throws<SerializationException>(() => serializer.Serialize(closure));

		StringAssert.Contains("Continuation", exception.Message, "the message should point at what to use instead");
	}

	// --- scalars ---

	[Test]
	public void EveryScalarTypeRoundTrips() {
		Assert.AreEqual(true, RoundTrip(new Scalar<bool> { Value = true }).Value);
		Assert.AreEqual((byte)200, RoundTrip(new Scalar<byte> { Value = 200 }).Value);
		Assert.AreEqual((sbyte)-100, RoundTrip(new Scalar<sbyte> { Value = -100 }).Value);
		Assert.AreEqual((short)-30000, RoundTrip(new Scalar<short> { Value = -30000 }).Value);
		Assert.AreEqual((ushort)60000, RoundTrip(new Scalar<ushort> { Value = 60000 }).Value);
		Assert.AreEqual(int.MinValue, RoundTrip(new Scalar<int> { Value = int.MinValue }).Value);
		Assert.AreEqual(uint.MaxValue, RoundTrip(new Scalar<uint> { Value = uint.MaxValue }).Value);
		Assert.AreEqual(long.MinValue, RoundTrip(new Scalar<long> { Value = long.MinValue }).Value);
		Assert.AreEqual(ulong.MaxValue, RoundTrip(new Scalar<ulong> { Value = ulong.MaxValue }).Value);
		Assert.AreEqual('x', RoundTrip(new Scalar<char> { Value = 'x' }).Value);
		Assert.AreEqual(1.5m, RoundTrip(new Scalar<decimal> { Value = 1.5m }).Value);
		Assert.AreEqual("hello", RoundTrip(new Scalar<string> { Value = "hello" }).Value);
		Assert.AreEqual(Guid.Empty, RoundTrip(new Scalar<Guid> { Value = Guid.Empty }).Value);
	}

	[Test]
	public void FloatsAndDoublesKeepTheirExactValue() {
		var awkwardFloat = 0.1f + 0.2f;
		var awkwardDouble = 1.0 / 3.0;

		Assert.AreEqual(awkwardFloat, RoundTrip(new Scalar<float> { Value = awkwardFloat }).Value,
			"a lossy number format would drift a save a little further every time it is loaded and re-saved");
		Assert.AreEqual(awkwardDouble, RoundTrip(new Scalar<double> { Value = awkwardDouble }).Value);
	}

	[Test]
	public void NotANumberIsRefusedRatherThanWrittenAsInvalidJson() {
		var story = new Scalar<float> { Value = float.NaN };

		var exception = Assert.Throws<SerializationException>(() => serializer.Serialize(story));

		StringAssert.Contains("NaN", exception.Message);
	}

	[Test]
	public void DateTimesKeepTheirKindAndPrecision() {
		var original = new DateTime(2026, 8, 27, 13, 45, 30, DateTimeKind.Utc).AddTicks(1234);

		var result = RoundTrip(new Scalar<DateTime> { Value = original }).Value;

		Assert.AreEqual(original, result);
		Assert.AreEqual(DateTimeKind.Utc, result.Kind);
	}

	[Test]
	public void EnumsRoundTripByName() {
		var json = serializer.Serialize(new WithEnum { Mood = Mood.Furious });

		StringAssert.Contains("Furious", json, "storing the name rather than the ordinal survives members being reordered");
		Assert.AreEqual(Mood.Furious, serializer.Deserialize<WithEnum>(json).Mood);
	}

	[Test]
	public void AnUnknownEnumMemberFailsLoudly() {
		var json = serializer.Serialize(new WithEnum { Mood = Mood.Calm }).Replace("Calm", "Removed");

		var exception = Assert.Throws<SerializationException>(() => serializer.Deserialize<WithEnum>(json));

		StringAssert.Contains("Removed", exception.Message);
	}

	// --- structure ---

	[Test]
	public void UnityStructsRoundTripThroughFieldReflection() {
		var result = RoundTrip(new WithVector { Position = new Vector3(1f, -2.5f, 3f) });

		Assert.AreEqual(new Vector3(1f, -2.5f, 3f), result.Position);
	}

	/// <summary>
	/// Unity value types that keep their state in private, unattributed fields. The field policy is "public or
	/// [SerializeField]", which selects none of them — <see cref="Rect"/> serialized as <c>{}</c> and read back
	/// as <c>(0, 0, 0, 0)</c>, with no error, because an empty object is valid JSON.
	/// <para>
	/// That reached a user: the Character Equipment sample saved its sprite rects, got zero rects back, and
	/// the equipment UI loaded blank. <see cref="Vector3"/> and <see cref="Color"/> only ever worked because
	/// their fields happen to be public, which is invisible from the outside.
	/// </para>
	/// </summary>
	[Test]
	public void UnityTypesWithPrivateStateRoundTrip() {
		Assert.AreEqual(new Rect(10f, 20f, 300f, 400f), RoundTrip(new Scalar<Rect> { Value = new Rect(10f, 20f, 300f, 400f) }).Value);
		Assert.AreEqual(new RectInt(1, 2, 3, 4), RoundTrip(new Scalar<RectInt> { Value = new RectInt(1, 2, 3, 4) }).Value);
		Assert.AreEqual(new Bounds(new Vector3(1, 2, 3), new Vector3(4, 5, 6)), RoundTrip(new Scalar<Bounds> { Value = new Bounds(new Vector3(1, 2, 3), new Vector3(4, 5, 6)) }).Value);
		Assert.AreEqual(new BoundsInt(1, 2, 3, 4, 5, 6), RoundTrip(new Scalar<BoundsInt> { Value = new BoundsInt(1, 2, 3, 4, 5, 6) }).Value);
		Assert.AreEqual(new Vector2Int(7, 8), RoundTrip(new Scalar<Vector2Int> { Value = new Vector2Int(7, 8) }).Value);
		Assert.AreEqual(new Vector3Int(7, 8, 9), RoundTrip(new Scalar<Vector3Int> { Value = new Vector3Int(7, 8, 9) }).Value);
		Assert.AreEqual(5, RoundTrip(new Scalar<LayerMask> { Value = (LayerMask)5 }).Value.value);
	}

	/// <summary>
	/// The general guard behind the fix above. A type that has state but none the policy can see is refused,
	/// so this class of bug cannot be silent again — the failure that made it dangerous was that an empty
	/// object is perfectly valid JSON and reads back as a default.
	/// </summary>
	[Test]
	public void ATypeWhoseStateCannotBeSeenIsRefused() {
		var exception = Assert.Throws<SerializationException>(
			() => serializer.Serialize(new Scalar<Plane> { Value = new Plane(Vector3.up, 2f) }));

		StringAssert.Contains("Plane", exception.Message);
		StringAssert.Contains("silently", exception.Message);
	}

	[Test]
	public void AGenuinelyEmptyTypeIsStillFine() {
		// The guard keys on "has fields, none selected" rather than "no fields", so a marker class is not
		// mistaken for data loss.
		Assert.IsNotNull(RoundTrip(new Marker()));
	}

	private class Marker {
	}

	[Test]
	public void CollectionsRoundTrip() {
		var original = new Collections {
			Numbers = new List<int> { 1, 2, 3 },
			Words = new[] { "a", "b" },
			Shapes = new Dictionary<string, Shape> { { "one", new Circle { Name = "one", Radius = 2f } } },
			Counts = new StringIntDictionary { { "gold", 10 } },
		};

		var result = RoundTrip(original);

		CollectionAssert.AreEqual(new[] { 1, 2, 3 }, result.Numbers);
		CollectionAssert.AreEqual(new[] { "a", "b" }, result.Words);
		Assert.AreEqual(2f, ((Circle)result.Shapes["one"]).Radius);
		Assert.AreEqual(10, result.Counts["gold"]);
	}

	[Test]
	public void SerializableDictionarySubclassesAreWrittenAsDictionariesNotBackingArrays() {
		// StringIntDictionary derives from Dictionary<,> several levels down. Matching the generic type
		// exactly would miss it and serialize SerializableDictionaryBase's keys/values arrays instead —
		// roughly double the size, through a path Unity only maintains for the inspector.
		var json = serializer.Serialize(new StringIntDictionary { { "gold", 10 } });

		StringAssert.Contains(NarramancerSerializer.EntriesKey, json);
		StringAssert.DoesNotContain("\"keys\"", json);
		StringAssert.DoesNotContain("\"values\"", json);
	}

	[Test]
	public void EmptyAndNullCollectionsStayDistinct() {
		var result = RoundTrip(new Collections { Numbers = new List<int>(), Words = null });

		Assert.IsNotNull(result.Numbers);
		Assert.IsEmpty(result.Numbers);
		Assert.IsNull(result.Words);
	}

	[Test]
	public void PolymorphicValuesKeepTheirConcreteType() {
		var result = RoundTrip(new Holder { Shape = new Circle { Name = "c", Radius = 3f } });

		Assert.IsInstanceOf<Circle>(result.Shape);
		Assert.AreEqual(3f, ((Circle)result.Shape).Radius);
	}

	[Test]
	public void ScalarsInObjectSlotsKeepTheirType() {
		// The blackboard is a StringObjectDictionary, so nearly every value in it lands in an object slot.
		Assert.AreEqual(5, RoundTrip(new Holder { Anything = 5 }).Anything);
		Assert.AreEqual("text", RoundTrip(new Holder { Anything = "text" }).Anything);
		Assert.AreEqual(2.5f, RoundTrip(new Holder { Anything = 2.5f }).Anything);
		Assert.AreEqual(true, RoundTrip(new Holder { Anything = true }).Anything);
	}

	[Test]
	public void SharedReferencesStaySharedAcrossTheRoundTrip() {
		var shared = new Simple { PublicField = 1 };
		var original = new Holder { First = shared, Second = shared };

		var json = serializer.Serialize(original);
		var result = serializer.Deserialize<Holder>(json);

		StringAssert.Contains(NarramancerSerializer.ReferenceKey, json, "the second sighting must be a back-reference, not a copy");
		Assert.AreSame(result.First, result.Second, "two copies would mean an edit through one stopped being visible through the other");
	}

	[Test]
	public void CyclesTerminateAndReconnect() {
		// NodeRunner to Promise to the runner table and back is exactly this shape.
		var first = new Ring { Name = "first" };
		var second = new Ring { Name = "second", Next = first };
		first.Next = second;

		var result = RoundTrip(first);

		Assert.AreEqual("second", result.Next.Name);
		Assert.AreSame(result, result.Next.Next);
	}

	[Test]
	public void TypesWithNoParameterlessConstructorAreReconstructed() {
		// None of v1's save types has one; SaveLoadRoundTripTests pins that fact.
		Assume.That(typeof(NoDefaultConstructor).GetConstructor(Type.EmptyTypes), Is.Null);

		Assert.AreEqual(9, RoundTrip(new NoDefaultConstructor(9)).Value);
	}

	[Test]
	public void SystemTypeValuesRoundTrip() {
		var result = RoundTrip(new WithType { Value = typeof(StringIntDictionary) });

		Assert.AreEqual(typeof(StringIntDictionary), result.Value);
	}

	[Test]
	public void NullsRoundTripAsNulls() {
		var result = RoundTrip(new Holder());

		Assert.IsNull(result.Shape);
		Assert.IsNull(result.Anything);
		Assert.IsNull(result.First);
	}

	// --- asset references ---

	[Test]
	public void UnityObjectsAreWrittenAsIdHandlesNotInlined() {
		var noun = Asset<NounScriptableObject>("Hero");
		var references = new InMemoryReferenceResolver().Register("noun-guid", noun);
		var withReferences = new NarramancerSerializer(references);

		var json = withReferences.Serialize(new WithAsset { Noun = noun });

		StringAssert.Contains(NarramancerSerializer.AssetKey, json);
		StringAssert.Contains("noun-guid", json);
		Assert.AreSame(noun, withReferences.Deserialize<WithAsset>(json).Noun);
	}

	/// <summary>
	/// The §6.3 policy, write half: an asset the resolver cannot name is dropped with a logged error rather
	/// than throwing. Throwing would fail at the worst possible moment in a shipped game — the moment the
	/// player pressed Save — and losing one blackboard entry beats losing the whole save. The read half stays
	/// fatal, because there an unresolvable id is indistinguishable from one resolving to the wrong thing.
	/// </summary>
	[Test]
	public void AnAssetTheResolverCannotNameIsDroppedWithAnError() {
		var noun = Asset<NounScriptableObject>("Hero");
		var withEmptyResolver = new NarramancerSerializer(new InMemoryReferenceResolver());

		UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error, new System.Text.RegularExpressions.Regex("Hero"));
		var json = withEmptyResolver.Serialize(new WithAsset { Noun = noun });

		Assert.IsNull(withEmptyResolver.Deserialize<WithAsset>(json).Noun);
		StringAssert.DoesNotContain(NarramancerSerializer.AssetKey, json, "the reference should be dropped, not written as a dangling handle");
	}

	/// <summary>The error has to name all three fixes, or it tells the author nothing actionable.</summary>
	[Test]
	public void TheDroppedReferenceErrorNamesTheWaysToFixIt() {
		var noun = Asset<NounScriptableObject>("Hero");

		UnityEngine.TestTools.LogAssert.Expect(UnityEngine.LogType.Error,
			new System.Text.RegularExpressions.Regex("Resources.*Additional Assets.*RuntimeAssetRegistry"));

		new NarramancerSerializer(new InMemoryReferenceResolver()).Serialize(new WithAsset { Noun = noun });
	}

	[Test]
	public void SerializingAUnityObjectWithoutAResolverFailsLoudly() {
		// Inlining it would bake a stale copy of authored content into the save.
		var noun = Asset<NounScriptableObject>("Hero");

		var exception = Assert.Throws<SerializationException>(() => serializer.Serialize(new WithAsset { Noun = noun }));

		StringAssert.Contains(nameof(IReferenceResolver), exception.Message);
	}

	[Test]
	public void AnUnresolvableAssetReferenceRefusesToLoad() {
		// The instanceID bug produced no error at all - it resumed the story at the wrong node. Refusing
		// the load is the only outcome a player can act on. See docs/ODIN_REMOVAL_PLAN.md section 2A.
		var noun = Asset<NounScriptableObject>("Hero");
		var writeSide = new NarramancerSerializer(new InMemoryReferenceResolver().Register("noun-guid", noun));
		var json = writeSide.Serialize(new WithAsset { Noun = noun });
		var readSide = new NarramancerSerializer(new InMemoryReferenceResolver());

		var exception = Assert.Throws<SerializationException>(() => readSide.Deserialize<WithAsset>(json));

		StringAssert.Contains("noun-guid", exception.Message);
	}

	[Test]
	public void ADestroyedUnityObjectIsWrittenAsNull() {
		var noun = ScriptableObject.CreateInstance<NounScriptableObject>();
		var withReferences = new NarramancerSerializer(new InMemoryReferenceResolver());
		UnityEngine.Object.DestroyImmediate(noun);

		var result = withReferences.Deserialize<WithAsset>(withReferences.Serialize(new WithAsset { Noun = noun }));

		Assert.IsNull(result.Noun);
	}

	// --- the save envelope ---

	[Test]
	public void TheSaveEnvelopeCarriesItsSchemaVersion() {
		var json = serializer.SerializeSave(new Simple { PublicField = 3 });

		var result = serializer.DeserializeSave<Simple>(json, out var version);

		Assert.AreEqual(NarramancerSerializer.CurrentSchemaVersion, version);
		Assert.AreEqual(3, result.PublicField);
	}

	[Test]
	public void ASaveFromANewerFormatIsRejectedWithAClearMessage() {
		var json = serializer.SerializeSave(new Simple())
			.Replace($"\"{NarramancerSerializer.SchemaVersionKey}\":{NarramancerSerializer.CurrentSchemaVersion}",
					 $"\"{NarramancerSerializer.SchemaVersionKey}\":{NarramancerSerializer.CurrentSchemaVersion + 1}");

		var exception = Assert.Throws<SerializationException>(() => serializer.DeserializeSave<Simple>(json, out _));

		StringAssert.Contains("newer version", exception.Message);
	}

	[Test]
	public void APayloadWithNoEnvelopeIsRejected() {
		var json = serializer.Serialize(new Simple());

		Assert.Throws<SerializationException>(() => serializer.DeserializeSave<Simple>(json, out _));
	}

	// --- type names ---

	[Test]
	public void StoredTypeNamesCarryNoAssemblyVersion() {
		// Unity bumps assembly versions for reasons unrelated to the plugin; a version in the stored name
		// would invalidate every type tag in every existing save when it did.
		var name = TypeNames.Write(typeof(StringIntDictionary));

		StringAssert.DoesNotContain("Version=", name);
		StringAssert.DoesNotContain("Culture=", name);
		StringAssert.DoesNotContain("PublicKeyToken=", name);
	}

	[Test]
	public void GenericTypeNamesRoundTripIncludingTheirArguments() {
		var types = new[] {
			typeof(int),
			typeof(string),
			typeof(List<NounInstance>),
			typeof(Dictionary<string, List<int>>),
			typeof(StringIntDictionary),
			typeof(Mood),
			typeof(Vector3[]),
		};

		foreach (var type in types) {
			Assert.AreEqual(type, TypeNames.Read(TypeNames.Write(type)), $"round trip failed for {type}");
		}
	}

	[Test]
	public void AnUnknownTypeNameFailsLoudly() {
		var exception = Assert.Throws<SerializationException>(() => TypeNames.Read("Narramancer.TypeThatWasDeleted, Narramancer"));

		StringAssert.Contains("TypeThatWasDeleted", exception.Message);
	}

	// --- the json layer ---

	[Test]
	public void JsonRoundTripsAwkwardStrings() {
		var awkward = "quote\" backslash\\ newline\n tab\t unicodeé control";

		var result = RoundTrip(new Scalar<string> { Value = awkward });

		Assert.AreEqual(awkward, result.Value);
	}

	[Test]
	public void MalformedJsonIsRejectedWithAPosition() {
		var exception = Assert.Throws<SerializationException>(() => JsonParser.Parse("{\"a\": }"));

		StringAssert.Contains("index", exception.Message);
	}

	[Test]
	public void ObjectMemberOrderIsStable() {
		var original = new Derived { BaseValue = 1, DerivedValue = 2 };

		Assert.AreEqual(serializer.Serialize(original), serializer.Serialize(original),
			"stable output is what makes two saves diffable and the golden-file check in the removal plan possible");
	}
}
