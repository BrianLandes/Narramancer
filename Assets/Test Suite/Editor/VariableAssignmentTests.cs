using System.Collections.Generic;
using Narramancer;
using NUnit.Framework;

/// <summary>
/// <see cref="VariableAssignmentExtensions.MatchToVariables"/>: keeping the values a component assigns to a
/// verb's inputs when those inputs change. It used to need the id <em>and</em> the name, so renaming an input
/// wiped the value assigned to it in every component that ran the verb, with nothing said.
/// </summary>
public class VariableAssignmentTests {

	private static NarramancerPort Port<T>(string name) {
		return new NarramancerPort { Name = name, Type = typeof(T) };
	}

	private static VariableAssignment AssignedTo(NarramancerPort port, int value) {
		return new VariableAssignment {
			id = port.Id,
			name = port.Name,
			type = VariableAssignment.TypeToString(port.Type),
			intValue = value,
		};
	}

	[Test]
	public void AnUnchangedInputKeepsItsValue() {
		var speed = Port<int>("speed");
		var assignments = new List<VariableAssignment> { AssignedTo(speed, 5) };

		assignments.MatchToVariables(new List<NarramancerPort> { speed });

		Assert.AreEqual(5, assignments[0].intValue);
	}

	[Test]
	public void ARenamedInputKeepsItsValue() {
		var speed = Port<int>("speed");
		var assignments = new List<VariableAssignment> { AssignedTo(speed, 5) };

		speed.Name = "pace";
		assignments.MatchToVariables(new List<NarramancerPort> { speed });

		Assert.AreEqual(5, assignments[0].intValue);
		Assert.AreEqual("pace", assignments[0].name, "the stale name is repaired");
	}

	[Test]
	public void AnInputRecreatedUnderTheSameNameKeepsItsValue() {
		var speed = Port<int>("speed");
		var assignments = new List<VariableAssignment> { AssignedTo(speed, 5) };

		speed.GenerateNewId();
		assignments.MatchToVariables(new List<NarramancerPort> { speed });

		Assert.AreEqual(5, assignments[0].intValue);
		Assert.AreEqual(speed.Id, assignments[0].id, "the stale id is repaired");
	}

	[Test]
	public void AnInputWhoseTypeChangedStartsOver() {
		var speed = Port<int>("speed");
		var assignments = new List<VariableAssignment> { AssignedTo(speed, 5) };

		speed.Type = typeof(float);
		assignments.MatchToVariables(new List<NarramancerPort> { speed });

		Assert.AreEqual(0, assignments[0].intValue);
		Assert.AreEqual("float", assignments[0].type);
	}

	/// <summary>
	/// The case a single "id or name" check gets wrong: <c>speed</c> renamed to <c>pace</c>, and a new
	/// <c>speed</c> added above it. Matching variables in order, the new one claims the old value by name.
	/// </summary>
	[Test]
	public void ANewInputTakingARenamedInputsOldNameDoesNotTakeItsValue() {
		var pace = Port<int>("speed");
		var assignments = new List<VariableAssignment> { AssignedTo(pace, 5) };

		pace.Name = "pace";
		var newSpeed = Port<int>("speed");
		assignments.MatchToVariables(new List<NarramancerPort> { newSpeed, pace });

		Assert.AreEqual(0, assignments[0].intValue, "the new input starts empty");
		Assert.AreEqual(5, assignments[1].intValue, "the renamed input keeps its value");
	}

	[Test]
	public void ARemovedInputDropsItsAssignment() {
		var speed = Port<int>("speed");
		var health = Port<int>("health");
		var assignments = new List<VariableAssignment> { AssignedTo(speed, 5), AssignedTo(health, 10) };

		assignments.MatchToVariables(new List<NarramancerPort> { health });

		Assert.AreEqual(1, assignments.Count);
		Assert.AreEqual(10, assignments[0].intValue);
	}
}
