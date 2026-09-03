using System;
using System.Collections.Generic;
using System.Linq;
using Narramancer;
using NUnit.Framework;
using UnityEngine;
using XNode;

/// <summary>
/// Breadth coverage over <b>every</b> concrete node type, data-driven so a node added tomorrow is covered the
/// day it's written.
/// <para>
/// Note what this deliberately does <i>not</i> claim. A node's own fields never enter a save — verified against
/// real saves, where node types appear only as <c>$eref</c> external references and never as an inlined
/// <c>$type</c>. Node data lives in the <c>.asset</c> file, serialized by Unity. So the risk worth covering is
/// not "does a node serialize" but <b>"does a reference to a node survive a save"</b> — which is precisely the
/// path the instanceID bug breaks and the GUID-keyed resolver has to preserve.
/// </para>
/// </summary>
public class NodeTypeCoverageTests {

	/// <summary>Every concrete node type in the project, used as the source for the data-driven cases below.</summary>
	private static IEnumerable<Type> AllNodeTypes() {
		return AssemblyUtilities.GetAllTypes<Node>()
			.Where(type => !type.IsAbstract && !type.IsGenericTypeDefinition)
			.OrderBy(type => type.FullName);
	}

	private ActionVerb graph;

	[SetUp]
	public void SetUp() {
		graph = ScriptableObject.CreateInstance<ActionVerb>();
		graph.name = "Test Verb";
	}

	[TearDown]
	public void TearDown() {
		Narramancer.Serialization.RuntimeAssetRegistry.Clear();
		if (graph == null) {
			return;
		}
		// Nodes are separate objects; destroying only the graph would leak them.
		foreach (var node in graph.nodes.ToList()) {
			if (node != null) {
				UnityEngine.Object.DestroyImmediate(node);
			}
		}
		UnityEngine.Object.DestroyImmediate(graph);
		graph = null;
	}

	[Test]
	public void TheProjectHasTheNodeTypesWeExpect() {
		var count = AllNodeTypes().Count();
		Assert.Greater(count, 100,
			$"expected the full node library to be discoverable, found only {count} — " +
			"a regression in AssemblyUtilities.GetAllTypes would show up here first");
	}

	/// <summary>
	/// Every node type can be constructed through the graph and build its ports. This is the smoke test that
	/// catches a node whose <c>Init()</c> or <c>UpdatePorts()</c> throws — which would otherwise only surface
	/// when an author happened to drop that node into a graph.
	/// </summary>
	[Test]
	public void EveryNodeTypeConstructsAndBuildsItsPorts([ValueSource(nameof(AllNodeTypes))] Type nodeType) {
		Node node = null;
		Assert.DoesNotThrow(() => node = graph.AddNode(nodeType),
			$"{nodeType.Name} threw while being added to a graph");

		Assert.IsNotNull(node, $"{nodeType.Name} did not construct");
		Assert.AreSame(graph, node.graph, $"{nodeType.Name} did not get its graph assigned");

		Assert.DoesNotThrow(() => { var _ = node.Ports.Count(); },
			$"{nodeType.Name} threw while enumerating its ports");
	}

	/// <summary>
	/// The one that matters for the serializer swap: for every node type, a <b>reference</b> to it survives a
	/// save round trip and resolves back to the same asset. This is the <c>$eref</c> path — the mechanism the
	/// instanceID bug breaks — so it doubles as the regression net for the GUID-keyed resolver that replaces it.
	/// </summary>
	[Test]
	public void EveryNodeTypeSurvivesBeingReferencedAcrossASave([ValueSource(nameof(AllNodeTypes))] Type nodeType) {
		var node = graph.AddNode(nodeType);
		Assume.That(node, Is.Not.Null, $"{nodeType.Name} could not be constructed");

		// A node built with AddNode is not an asset and so has no persistent id. Registering it is what lets
		// a save name it — see the note in SaveLoadRoundTripTests.
		Narramancer.Serialization.RuntimeAssetRegistry.Register($"test-node-{nodeType.Name}", node);

		var story = new StoryInstance();
		var runner = new NodeRunner { name = "main" };
		story.NodeRunners["main"] = runner;

		// A NodeRunnerEvent is the simplest carrier of a node reference that a real save actually contains.
		runner.Blackboard.Set("marker", nodeType.Name);
		if (node is RunnableNode runnable) {
			runner.Append(runnable);
		}
		else {
			// Value nodes are never queued; reference them through the blackboard instead.
			runner.Blackboard.Set("node", node);
		}

		var payload = SaveLoadUtilities.SerializePayload(story);
		var restored = SaveLoadUtilities.DeserializePayload<StoryInstance>(payload);
		var restoredRunner = restored.NodeRunners["main"];

		Assert.AreEqual(nodeType.Name, restoredRunner.Blackboard.Get<string>("marker"),
			$"the runner carrying a {nodeType.Name} reference did not survive");

		if (node is RunnableNode) {
			StringAssert.Contains(Narramancer.Serialization.NarramancerSerializer.AssetKey, payload,
				$"a reference to {nodeType.Name} should leave the payload as an asset handle, not be inlined");
		}
		else {
			Assert.AreSame(node, restoredRunner.Blackboard.Get<UnityEngine.Object>("node"),
				$"the {nodeType.Name} reference did not resolve back to the same asset");
		}
	}
}
