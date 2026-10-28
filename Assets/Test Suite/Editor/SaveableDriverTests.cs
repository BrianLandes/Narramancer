using System.Collections.Generic;
using System.Linq;
using Narramancer;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// <see cref="Saveable"/> as the one component that saves an object: its drivers (<see cref="IComponentSaver"/>)
/// for component state, and the <see cref="SaveAttribute"/> fields of behaviour components such as
/// <see cref="Tweener"/>.
/// <para>
/// As in <see cref="SceneComponentSaveTests"/>, each round trip saves from one object and loads into a
/// different one answering to the same guid, because a load reloads the scene.
/// </para>
/// </summary>
public class SaveableDriverTests {

	private readonly List<Object> created = new List<Object>();

	/// <summary>Assets outlive a simulated scene unload, as they would a real one.</summary>
	private readonly List<Object> assets = new List<Object>();

	[SetUp]
	public void SetUp() {
		Saveable.ClearClaims();
	}

	[TearDown]
	public void TearDown() {
		UnloadScene();
		foreach (var asset in assets) {
			if (asset != null) {
				Object.DestroyImmediate(asset);
			}
		}
		assets.Clear();
		Narramancer.Serialization.RuntimeAssetRegistry.Clear();
	}

	private void UnloadScene() {
		foreach (var obj in created) {
			if (obj != null) {
				Object.DestroyImmediate(obj);
			}
		}
		created.Clear();
		Saveable.ClearClaims();
	}

	/// <summary>An object with a Saveable set up as adding it by hand would, saving everything it detects.</summary>
	private Saveable Object_(string name, string guid = null, params System.Type[] components) {
		var gameObject = new GameObject(name, components);
		created.Add(gameObject);
		var saveable = gameObject.AddComponent<Saveable>();
		if (guid != null) {
			saveable.AssignGuid(guid);
		}
		else {
			saveable.EnsureGuid();
		}
		saveable.SaveEverythingDetected();
		return saveable;
	}

	private static StoryInstance Save(Saveable saveable) {
		var story = new StoryInstance { SaveTable = new Blackboard() };
		saveable.Serialize(story);
		return SaveLoadUtilities.DeserializePayload<StoryInstance>(SaveLoadUtilities.SerializePayload(story));
	}

	#region The registry

	[Test]
	public void TheMostSpecificDriverWins() {
		Assert.IsTrue(ComponentSaverRegistry.TryFind(typeof(RectTransform), out var rect));
		Assert.IsInstanceOf<RectTransformSaver>(rect, "RectTransform derives from Transform, and must not get the Transform driver");

		Assert.IsTrue(ComponentSaverRegistry.TryFind(typeof(Transform), out var plain));
		Assert.IsInstanceOf<TransformSaver>(plain);
	}

	private class GameImage : UnityEngine.UI.Image { }

	[Test]
	public void ASubclassGetsItsBaseTypesDriver() {
		Assert.IsTrue(ComponentSaverRegistry.TryFind(typeof(GameImage), out var saver));
		Assert.IsInstanceOf<ImageSaver>(saver);
	}

	private class Impostor : ComponentSaver<Light, Impostor.State> {
		public class State { }
		public override string Key => "transform";
		public override State Capture(Light light) => new State();
		public override void Apply(Light light, State state) { }
	}

	[Test]
	public void TwoDriversCannotShareAKey() {
		Assert.Throws<System.ArgumentException>(() => ComponentSaverRegistry.Register(new Impostor()),
			"two drivers on one key would overwrite each other's saves");
	}

	#endregion

	#region What gets saved

	/// <summary>
	/// The migration tool adds a Saveable to objects purely so they can be named — a UI Text a verb writes
	/// into, a printer. Those must not quietly start saving and restoring their transforms.
	/// </summary>
	[Test]
	public void AnIdentityOnlySaveableSavesNothing() {
		var saveable = Object_("Label");
		saveable.SaveIdentityOnly();
		saveable.transform.position = new Vector3(5, 5, 5);
		var guid = saveable.Guid;

		var restored = Save(saveable);
		UnloadScene();

		var loaded = Object_("Label", guid);
		loaded.SaveIdentityOnly();
		loaded.transform.position = Vector3.one;
		loaded.Deserialize(restored);

		Assert.AreEqual(Vector3.one, loaded.transform.position, "identity only: the scene's position stands");
	}

	[Test]
	public void AnUntickedComponentIsNotRestored() {
		var saveable = Object_("Sign", null, typeof(UnityEngine.UI.Text));
		saveable.GetComponent<UnityEngine.UI.Text>().text = "saved";
		ComponentSaverRegistry.TryFind(typeof(UnityEngine.UI.Text), out var textSaver);
		saveable.SetSaved(textSaver, false);
		var guid = saveable.Guid;

		var restored = Save(saveable);
		UnloadScene();

		var loaded = Object_("Sign", guid, typeof(UnityEngine.UI.Text));
		loaded.GetComponent<UnityEngine.UI.Text>().text = "scene";
		loaded.Deserialize(restored);

		Assert.AreEqual("scene", loaded.GetComponent<UnityEngine.UI.Text>().text);
	}

	#endregion

	#region Drivers round-trip

	/// <summary>Local, not world: the old saver wrote world values, which depended on the parent being restored first.</summary>
	[Test]
	public void ATransformComesBackWhereItWas() {
		var saveable = Object_("Crate");
		saveable.transform.localPosition = new Vector3(1, 2, 3);
		saveable.transform.localRotation = Quaternion.Euler(0, 45, 0);
		saveable.transform.localScale = new Vector3(2, 2, 2);
		var guid = saveable.Guid;

		var restored = Save(saveable);
		UnloadScene();

		var loaded = Object_("Crate", guid);
		loaded.Deserialize(restored);

		Assert.AreEqual(new Vector3(1, 2, 3), loaded.transform.localPosition);
		Assert.That(Quaternion.Angle(Quaternion.Euler(0, 45, 0), loaded.transform.localRotation), Is.LessThan(0.01f));
		Assert.AreEqual(new Vector3(2, 2, 2), loaded.transform.localScale);
	}

	[Test]
	public void TextAndActiveStateComeBack() {
		var saveable = Object_("Caption", null, typeof(UnityEngine.UI.Text));
		saveable.GetComponent<UnityEngine.UI.Text>().text = "Chapter Two";
		saveable.gameObject.SetActive(false);
		var guid = saveable.Guid;

		var restored = Save(saveable);
		UnloadScene();

		var loaded = Object_("Caption", guid, typeof(UnityEngine.UI.Text));
		loaded.Deserialize(restored);

		Assert.AreEqual("Chapter Two", loaded.GetComponent<UnityEngine.UI.Text>().text);
		Assert.IsFalse(loaded.gameObject.activeSelf, "it was saved hidden");
	}

	/// <summary>Two components using one driver on one object get two entries, not one overwriting the other.</summary>
	[Test]
	public void TwoAudioSourcesKeepTheirOwnSettings() {
		var saveable = Object_("Speaker");
		var first = saveable.gameObject.AddComponent<AudioSource>();
		var second = saveable.gameObject.AddComponent<AudioSource>();
		saveable.SaveEverythingDetected();
		first.volume = 0.25f;
		second.volume = 0.75f;
		var guid = saveable.Guid;

		var restored = Save(saveable);
		UnloadScene();

		var loaded = Object_("Speaker", guid);
		loaded.gameObject.AddComponent<AudioSource>();
		loaded.gameObject.AddComponent<AudioSource>();
		loaded.SaveEverythingDetected();
		loaded.Deserialize(restored);

		var sources = loaded.GetComponents<AudioSource>();
		Assert.AreEqual(0.25f, sources[0].volume);
		Assert.AreEqual(0.75f, sources[1].volume);
	}

	#endregion

	#region Sprites

	private Sprite NewSprite(string name) {
		var texture = new Texture2D(8, 8);
		assets.Add(texture);
		var sprite = Sprite.Create(texture, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 32f, 0, SpriteMeshType.Tight, new Vector4(1, 2, 3, 4));
		sprite.name = name;
		assets.Add(sprite);
		return sprite;
	}

	/// <summary>
	/// The old savers rebuilt every sprite with <c>Sprite.Create</c>, so a load produced a new sprite — not the
	/// authored one — and anything comparing sprites by reference broke. A nameable sprite now comes back as
	/// itself.
	/// </summary>
	[Test]
	public void ANameableSpriteComesBackAsItself() {
		var sprite = NewSprite("Portrait");
		Narramancer.Serialization.RuntimeAssetRegistry.Register("test-portrait", sprite);
		var saveable = Object_("Frame", null, typeof(SpriteRenderer));
		saveable.GetComponent<SpriteRenderer>().sprite = sprite;
		var guid = saveable.Guid;

		var restored = Save(saveable);
		UnloadScene();

		var loaded = Object_("Frame", guid, typeof(SpriteRenderer));
		loaded.Deserialize(restored);

		Assert.AreSame(sprite, loaded.GetComponent<SpriteRenderer>().sprite);
	}

	/// <summary>A sprite that cannot be named but whose texture can is rebuilt — now keeping its border and pixels per unit.</summary>
	[Test]
	public void ASpriteBuiltAtRuntimeIsRebuiltFaithfully() {
		var sprite = NewSprite("Thumbnail");
		Narramancer.Serialization.RuntimeAssetRegistry.Register("test-thumbnail-texture", sprite.texture);

		var saved = SavedSprite.From(sprite, sprite);
		var rebuilt = saved.ToSprite();
		assets.Add(rebuilt);

		Assert.AreNotSame(sprite, rebuilt);
		Assert.AreEqual(sprite.border, rebuilt.border, "the old savers dropped the border");
		Assert.AreEqual(sprite.pixelsPerUnit, rebuilt.pixelsPerUnit, "the old savers dropped pixels per unit");
		Assert.AreEqual(sprite.pivot, rebuilt.pivot);
	}

	/// <summary>Writing an unnameable reference would fail the entire save. One sprite is not worth that.</summary>
	[Test]
	public void AnUnnameableSpriteIsSkippedNotFatal() {
		var sprite = NewSprite("Loose");
		var saveable = Object_("Frame", null, typeof(SpriteRenderer));
		saveable.GetComponent<SpriteRenderer>().sprite = sprite;

		LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Loose.*cannot be saved"));
		Assert.DoesNotThrow(() => Save(saveable));
	}

	#endregion

	#region Behaviour components

	/// <summary>
	/// A node adds a Tweener mid-story, so after a load — the scene reloaded from disk — the object has none.
	/// The Saveable has to put it back before restoring its fields, or the move is lost and a verb waiting on
	/// it never resumes.
	/// </summary>
	[Test]
	public void ATweenerAddedMidStoryIsPutBackOnLoad() {
		var saveable = Object_("Cart");
		var promise = Tweener.On(saveable.gameObject).MoveTo(new Vector3(10, 0, 0), 1f);
		var guid = saveable.Guid;

		var restored = Save(saveable);
		UnloadScene();

		var loaded = Object_("Cart", guid);
		Assert.IsNull(loaded.GetComponent<Tweener>(), "precondition: the reloaded scene has no Tweener");
		loaded.Deserialize(restored);

		var tweener = loaded.GetComponent<Tweener>();
		Assert.IsNotNull(tweener, "the Tweener should be re-added");
		Assert.IsTrue(tweener.IsMoving, "and still be moving");
		Assert.AreEqual(1, restored.Promises.Count, "the promise a verb waits on is filed with the story's promises");

		tweener.Step(100f);

		Assert.AreEqual(new Vector3(10, 0, 0), loaded.transform.position);
		Assert.IsTrue(restored.Promises.Values.First().Resolved,
			"arriving resolves the restored promise, which is what resumes the verb");
	}

	/// <summary>
	/// A rotation tween stopped when <c>Quaternion.Dot</c> was approximately 0 — which is 90 degrees <em>apart</em>,
	/// not arrived — so it almost never finished and a verb waiting on it hung.
	/// </summary>
	[Test]
	public void ARotationTweenFinishes() {
		var gameObject = new GameObject("Turret");
		created.Add(gameObject);
		var tweener = Tweener.On(gameObject);
		var promise = tweener.RotateTo(Quaternion.Euler(0, 180, 0), 5f);

		for (var i = 0; i < 200 && tweener.IsRotating; i++) {
			tweener.Step(0.1f);
		}

		Assert.IsFalse(tweener.IsRotating, "the tween should arrive");
		Assert.IsTrue(promise.Resolved);
	}

	/// <summary>
	/// A component that saves itself — every <see cref="ISerializableMonoBehaviour"/> — must not have its
	/// <see cref="SaveAttribute"/> fields saved a second time by the Saveable.
	/// </summary>
	[Test]
	public void ASelfSavingComponentIsNotSavedTwice() {
		var saveable = Object_("Door");
		var verbRunner = saveable.gameObject.AddComponent<RunActionVerbMonoBehaviour>();
		typeof(RunActionVerbMonoBehaviour)
			.GetField("runner", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
			.SetValue(verbRunner, new NodeRunner { name = "door" });

		var story = new StoryInstance { SaveTable = new Blackboard() };
		saveable.Serialize(story);

		Assert.IsEmpty(story.NodeRunners, "the runner is the component's to save, under its own key");
	}

	#endregion
}
