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

		[Input(ShowBackingValue.Never, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField]
		SerializableSpawner spawner = default;

		[SerializeField]
		private List<PropertyAssignment> properties = new List<PropertyAssignment>();

		[SerializeField]
		private List<StatAssignment> stats = new List<StatAssignment>();

		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
        [SerializeField]
        private NounInstance instance = default;

		private string InstanceKey => Blackboard.UniqueKey(this, "Instance");

		public class Instancable : IInstancable {
			public string DisplayName { get; set; }

			public NounUID ID { get; set; }

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
				Properties = properties,
				Stats = stats,
			};

			var instance = NarramancerSingleton.Instance.CreateInstance(instancable);
			runner.Blackboard.Set(InstanceKey, instance);

			var spawner = GetInputValue(runner.Blackboard, nameof(this.spawner), this.spawner);
			if (spawner != null) {
				var newGameObject = spawner.Spawn();
				instance.GameObject = newGameObject;
			}
		}

		public override object GetValue(INodeContext context, NodePort port) {
			if ( Application.isPlaying && port.fieldName.Equals(nameof(instance))) {
				var blackboard = context as Blackboard;
				return blackboard.Get<NounInstance>(InstanceKey);
			}
			return base.GetValue(context, port);
		}
	}
}