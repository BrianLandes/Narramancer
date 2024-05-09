using System.Collections.Generic;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("Noun/Get Noun Ingredient")]
	public class GetNounIngredientNode : AbstractInstanceInputNode {


		[SerializeField]
		private SerializableType ingredientType = new SerializableType();

		private static string VALUE_OUTPUT = "value";

		protected override void Init() {
			ingredientType.OnChanged -= RebuildPorts;
			ingredientType.OnChanged += RebuildPorts;

			ingredientType.canBeList = false;

			ingredientType.typeFilter = type => typeof(AbstractNounIngredient).IsAssignableFrom(type);
		}

		private void RebuildPorts() {

			if (ingredientType.Type == null) {
				ClearDynamicPorts();
				return;
			}

			var outputValuePort = this.GetOrAddDynamicOutput(ingredientType.Type, VALUE_OUTPUT, false, false);
			this.ClearDynamicPortsExcept(outputValuePort);

		}


		public override object GetValue(INodeContext context, NodePort port) {

			if (Application.isPlaying && port.fieldName.Equals(VALUE_OUTPUT)) {
				var instance = GetInstance(context);
				var value = instance?.GetIngredient(ingredientType.Type);
				return value;
			}

			return base.GetValue(context, port);
		}
	}
}