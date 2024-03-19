

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using XNode;

namespace Narramancer {

	public class ChooseSpriteBasedOnFloatNode : Node {

		[SerializeField]
		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		float value = 0f;
		public static string ValueFieldName => nameof(value);

		[Serializable]
		public class Pairing {
			public Vector2 range;
			public string portName;
		}

		[SerializeField]
		private List<Pairing> pairings = new List<Pairing>();
		public static string PairingsFieldName => nameof(pairings);


		[Input(ShowBackingValue.Unconnected, ConnectionType.Override, TypeConstraint.Inherited)]
		[SerializeField]
		private Sprite defaultSprite = default;
		public static string DefaultSpriteFieldName => nameof(defaultSprite);

		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
		[SerializeField]
		private Sprite sprite = default;
		public static string SpriteFieldName => nameof(sprite);

		public override object GetValue(INodeContext context, NodePort port) {
			if ( port.fieldName.Equals(nameof(sprite))) {

				var value = GetInputValue(context, nameof(this.value), this.value);

				foreach( var pairing in pairings) {
					if (value >= pairing.range.x && value <= pairing.range.y ) {
						var inputValue = this.GetInputValue(context, pairing.portName);
						return inputValue;
					}
				}

				var defaultSprite = this.GetInputValue(context, nameof(this.defaultSprite), this.defaultSprite);
				return defaultSprite;
			}
			return null;
		}

		public Pairing AddNewPairing() {
			var newPairing = new Pairing();
			var newPort = this.AddDynamicInput(typeof(Sprite), ConnectionType.Override, TypeConstraint.Inherited);
			newPairing.portName = newPort.fieldName;
			pairings.Add(newPairing);
			return newPairing;
		}

		public void RemovePairing(int index) {
			var pairing = pairings[index];
			RemovePairing(pairing);
		}

		public void RemovePairing(Pairing pairing) {
			pairings.Remove(pairing);
			RemoveDynamicPort(pairing.portName);
		}

		public void DistributeEvenly() {
			if ( pairings.IsEmpty() ) {
				return;
			}
			var min = pairings.Select(x => x.range.x).OrderBy(x => x).FirstOrDefault();
			var max = pairings.Select(x => x.range.y).OrderByDescending(x => x).FirstOrDefault();

			var range = max - min;
			var step = range / pairings.Count();

			var threshold = min;
			foreach( var pairing in pairings) {
				pairing.range.x = threshold;
				threshold += step;
				pairing.range.y = threshold;
			}
		}
	}
}
