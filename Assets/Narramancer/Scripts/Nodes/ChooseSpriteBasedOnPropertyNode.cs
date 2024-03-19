

using System;
using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {

	public class ChooseSpriteBasedOnPropertyNode : AbstractInstanceInputNode {

		[Serializable]
		public class Pairing {
			public PropertyScriptableObject property;
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
			if (Application.isPlaying && port.fieldName.Equals(nameof(sprite))) {

				var instance = GetInstance(context);

				foreach( var pairing in pairings) {
					if (instance.HasProperty(pairing.property)) {
						var inputValue = this.GetInputValue(context, pairing.portName);
						return inputValue;
					}
				}

				var defaultSprite = this.GetInputValue(context, nameof(this.defaultSprite), this.defaultSprite);
				return defaultSprite;
			}
			return base.GetValue(context, port);
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
	}
}
