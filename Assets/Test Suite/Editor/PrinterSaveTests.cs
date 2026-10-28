using System.Collections.Generic;
using System.Reflection;
using Narramancer;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// What the text printers show after a load. These are the moment the plugin is sold on — a story resuming
/// mid-scene — so a printer that comes back looking different from how it was saved is a visible bug even
/// when the story underneath is right.
/// <para>
/// Same shape as <see cref="SceneComponentSaveTests"/>: save from one printer, load into a fresh one answering
/// to the same guid, with <c>Serialize</c> / <c>Deserialize</c> called directly.
/// </para>
/// </summary>
public class PrinterSaveTests {

	private const string Guid = "0b3f6a1d2c4e4f5a8b9c0d1e2f3a4b5c";

	private readonly List<Object> created = new List<Object>();

	[SetUp]
	public void SetUp() {
		Saveable.ClearClaims();
	}

	[TearDown]
	public void TearDown() {
		UnloadScene();
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

	private GameObject New(string name) {
		var gameObject = new GameObject(name);
		created.Add(gameObject);
		return gameObject;
	}

	private static void SetField(object target, string field, object value) {
		var type = target.GetType();
		FieldInfo info = null;
		for (; info == null && type != null; type = type.BaseType) {
			info = type.GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
		}
		Assert.IsNotNull(info, $"no field '{field}' on {target.GetType().Name}");
		info.SetValue(target, value);
	}

	/// <summary>A printer wired the way the shipped prefabs are, with the continue indicator showing or not.</summary>
	private T Printer<T>(bool indicatorShowing) where T : TextPrinter {
		var gameObject = New("Printer");
		gameObject.AddComponent<Saveable>().AssignGuid(Guid);
		var printer = gameObject.AddComponent<T>();
		var indicator = New("Continue Indicator");
		indicator.SetActive(indicatorShowing);
		SetField(printer, "textField", New("Text").AddComponent<Text>());
		SetField(printer, "continueIndicator", indicator);
		SetField(printer, "parentCanvasGroup", New("Canvas").AddComponent<CanvasGroup>());
		if (printer is TextAndChoicePrinter) {
			SetField(printer, "toolTipView", New("Tooltip"));
		}
		return printer;
	}

	private static GameObject IndicatorOf(TextPrinter printer) {
		return (GameObject)typeof(TextPrinter)
			.GetField("continueIndicator", BindingFlags.Instance | BindingFlags.NonPublic)
			.GetValue(printer);
	}

	/// <summary>Saves a printer showing <paramref name="text"/>, waiting on a click or not, and loads it back.</summary>
	private T SaveAndLoad<T>(string text, bool waitingForContinue) where T : TextPrinter {
		var saving = Printer<T>(indicatorShowing: waitingForContinue);
		SetField(saving, "targetText", text);
		SetField(saving, "promise", waitingForContinue ? new Promise() : null);
		var story = new StoryInstance { SaveTable = new Blackboard() };
		saving.Serialize(story);
		story = SaveLoadUtilities.DeserializePayload<StoryInstance>(SaveLoadUtilities.SerializePayload(story));

		UnloadScene();

		// A freshly loaded scene: the indicator in whatever state the prefab has it.
		var loading = Printer<T>(indicatorShowing: true);
		loading.Deserialize(story);
		return loading;
	}

	/// <summary>
	/// The reported bug: a sample printed text, was clicked on, and moved into a <c>Wait</c>. The text-and-choice
	/// printer keeps its text up after a click, like a log, so a load showed that text with the continue indicator
	/// — inviting a click that did nothing while the timer ran out.
	/// </summary>
	[Test]
	public void TextLeftUpDuringAWaitDoesNotAskForAClickAfterALoad() {
		var printer = SaveAndLoad<TextAndChoicePrinter>("The lantern gutters.", waitingForContinue: false);

		Assert.IsFalse(IndicatorOf(printer).activeSelf);
	}

	[Test]
	public void TextPrintedWithoutWaitingDoesNotAskForAClickAfterALoad() {
		var printer = SaveAndLoad<TextPrinter>("The lantern gutters.", waitingForContinue: false);

		Assert.IsFalse(IndicatorOf(printer).activeSelf);
	}

	/// <summary>The other half: a printer that really is waiting on a click must still say so.</summary>
	[Test]
	public void TextWaitingOnAClickStillAsksForOneAfterALoad() {
		var printer = SaveAndLoad<TextPrinter>("The lantern gutters.", waitingForContinue: true);

		Assert.IsTrue(IndicatorOf(printer).activeSelf);
	}

	[Test]
	public void SkippingTheRevealOfTextThatDoesNotWaitShowsNoIndicator() {
		var printer = Printer<TextPrinter>(indicatorShowing: false);
		SetField(printer, "targetText", "The lantern gutters.");

		printer.SkipTextReveal();

		Assert.IsFalse(IndicatorOf(printer).activeSelf);
	}
}
