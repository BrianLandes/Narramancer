using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("Noun/Create Instance")]
	public class CreateInstanceNode : ChainedRunnableNode {

		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField]
		private string displayName = "";

		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField]
		[NodeEnum]
		private NounType nounType = NounType.Character;

		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField]
		[NodeEnum]
		private Pronouns pronouns = Pronouns.Nonbinary;

		[SerializeField]
		private List<PropertyScriptableObject> properties = new List<PropertyScriptableObject>();

		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
        [SerializeField]
        private NounInstance instance = default;

		private string InstanceKey => Blackboard.UniqueKey(this, "Instance");

		public class Instancable : IInstancable {
			public string DisplayName { get; set; }
			public NounType NounType { get; set; }

			public NounUID ID { get; set; }

			public Pronouns Pronouns { get; set; }

			public IEnumerable<PropertyAssignment> Properties { get; set; } = Enumerable.Empty<PropertyAssignment>();

			public IEnumerable<StatAssignment> Stats { get; set; } = Enumerable.Empty<StatAssignment>();

			public IEnumerable<RelationshipAssignment> Relationships { get; set; } = Enumerable.Empty<RelationshipAssignment>();

			public Blackboard Blackboard { get; set; } = null;
		}

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			var instancable = new Instancable() {
				DisplayName = GetInputValue(runner.Blackboard, nameof(displayName), displayName),
				ID = new NounUID(),
				NounType = GetInputValue(runner.Blackboard, nameof(nounType), nounType),
				Pronouns = GetInputValue(runner.Blackboard, nameof(pronouns), pronouns),
				Properties = properties.Select( property => new PropertyAssignment() { property = property }),
			};

			var instance = NarramancerSingleton.Instance.CreateInstance(instancable);
			runner.Blackboard.Set(InstanceKey, instance);
		}

		public override object GetValue(object context, NodePort port) {
			if ( Application.isPlaying && port.fieldName.Equals(nameof(instance))) {
				var blackboard = context as Blackboard;
				return blackboard.Get<NounInstance>(InstanceKey);
			}
			return base.GetValue(context, port);
		}
	}
}