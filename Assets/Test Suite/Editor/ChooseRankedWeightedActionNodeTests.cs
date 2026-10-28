using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Narramancer;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// <see cref="ChooseRankedWeightedActionNode"/> forwarding its actions' effect-graph inputs. It used to grow no
/// ports for them and assign none, so the node only worked with effect graphs that took nothing but the instance.
/// </summary>
public class ChooseRankedWeightedActionNodeTests {

	private readonly List<Object> created = new List<Object>();
	private ActionVerb graph;
	private ChooseRankedWeightedActionNode node;

	[SetUp]
	public void SetUp() {
		graph = New<ActionVerb>("Host");
		node = graph.AddNode<ChooseRankedWeightedActionNode>();
		created.Add(node);
	}

	[TearDown]
	public void TearDown() {
		foreach (var obj in created) {
			if (obj != null) {
				Object.DestroyImmediate(obj);
			}
		}
		created.Clear();
	}

	private T New<T>(string name) where T : ScriptableObject {
		var asset = ScriptableObject.CreateInstance<T>();
		asset.name = name;
		created.Add(asset);
		return asset;
	}

	/// <summary>An action whose effect graph takes the instance plus each of <paramref name="inputs"/>.</summary>
	private RankedWeightedAction Action(params (System.Type type, string name)[] inputs) {
		var effect = New<ActionVerb>("Effect");
		effect.AddInput(typeof(NounInstance), "instance");
		foreach (var (type, name) in inputs) {
			effect.AddInput(type, name);
		}
		var action = New<RankedWeightedAction>("Action");
		typeof(RankedWeightedAction).GetField("effectGraph", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(action, effect);
		node.actions.Add(action);
		return action;
	}

	private Dictionary<string, System.Type> DynamicInputs() {
		return node.DynamicInputs.ToDictionary(port => port.fieldName, port => port.ValueType);
	}

	[Test]
	public void AnEffectGraphsInputsBecomePortsButTheInstanceDoesNot() {
		Action((typeof(int), "speed"));

		node.UpdatePorts();

		CollectionAssert.AreEquivalent(new Dictionary<string, System.Type> { ["speed"] = typeof(int) }, DynamicInputs(),
			"the instance comes from the node's own input, so it gets no second port");
	}

	[Test]
	public void InputsWithTheSameNameAndTypeShareAPort() {
		Action((typeof(int), "speed"));
		Action((typeof(int), "speed"));

		node.UpdatePorts();

		CollectionAssert.AreEquivalent(new Dictionary<string, System.Type> { ["speed"] = typeof(int) }, DynamicInputs());
	}

	[Test]
	public void InputsWithTheSameNameButDifferentTypesGetAPortEach() {
		Action((typeof(int), "speed"));
		Action((typeof(float), "speed"));

		node.UpdatePorts();

		CollectionAssert.AreEquivalent(new Dictionary<string, System.Type> {
			["speed (Int32)"] = typeof(int),
			["speed (Single)"] = typeof(float),
		}, DynamicInputs());
	}

	/// <summary>
	/// At run time the node looks up the ports of the one action it chose. Those names have to come out the same
	/// as when every action built the ports, or the chosen action would read from a port that is not there.
	/// </summary>
	[Test]
	public void TheChosenActionsInputsResolveToThePortsTheNodeBuilt() {
		Action((typeof(int), "speed"));
		var chosen = Action((typeof(float), "speed"), (typeof(string), "line"));
		node.UpdatePorts();

		var forChosen = node.ForwardedInputs(new[] { chosen }).ToList();

		CollectionAssert.AreEquivalent(new[] { "speed (Single)", "line" }, forChosen.Select(pair => pair.portName));
		foreach (var (input, portName) in forChosen) {
			Assert.AreEqual(input.Type, node.GetInputPort(portName)?.ValueType, portName);
		}
	}

	[Test]
	public void AnInputNamedLikeOneOfTheNodesOwnPortsDoesNotCollideWithIt() {
		Action((typeof(int), AbstractInstanceInputNode.NounInstanceField));

		Assert.DoesNotThrow(() => node.UpdatePorts());

		Assert.AreEqual(typeof(int), node.GetInputPort("instance (Int32)")?.ValueType);
	}

	[Test]
	public void RemovingAnActionRemovesItsPorts() {
		var action = Action((typeof(int), "speed"));
		node.UpdatePorts();

		node.actions.Remove(action);
		node.UpdatePorts();

		CollectionAssert.IsEmpty(DynamicInputs());
	}
}
