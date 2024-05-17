using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using XNode;

namespace Narramancer {
	[CreateNodeMenu("Noun/Get Noun Ingredient")]
	public class GetNounIngredientNode : AbstractInstanceInputNode {


		[SerializeField]
		private SerializableType ingredientType = new SerializableType();

		[SerializeField]
		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
		bool hasIngredient = false;

		private static string VALUE_OUTPUT = "ingredient";

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

			var keepPorts = new List<NodePort>();

			var outputValuePort = this.GetOrAddDynamicOutput(ingredientType.Type, VALUE_OUTPUT, false, false);
			keepPorts.Add(outputValuePort);

			var fields = ingredientType.Type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.DeclaredOnly);

			foreach (var field in fields) {
				var outputPort = this.GetOrAddDynamicOutput(field.FieldType, field.Name);
				keepPorts.Add(outputPort);
			}

			var methods = ingredientType.Type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.DeclaredOnly);

			foreach (var method in methods) {
				if (method.GetParameters().Any() || method.IsGenericMethod || method.ReturnType == typeof(void)) {
					continue;
				}

				var outputPort = this.GetOrAddDynamicOutput(method.ReturnType, method.Name);
				keepPorts.Add(outputPort);
			}


			this.ClearDynamicPortsExcept(keepPorts);
		}


		public override object GetValue(INodeContext context, NodePort port) {

			if (Application.isPlaying) {
				var instance = GetInstance(context);
				var ingredient = instance?.GetIngredient(ingredientType.Type);

				if (port.fieldName.Equals(VALUE_OUTPUT)) {
					return ingredient;
				}
				if (port.fieldName.Equals(nameof(hasIngredient))) {
					return ingredient != null;
				}

				var fields = ingredientType.Type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.DeclaredOnly);
				foreach (var field in fields) {
					if (port.fieldName.Equals(field.Name, System.StringComparison.Ordinal)) {
						return field.GetValue(ingredient);
					}
				}

				var methods = ingredientType.Type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.DeclaredOnly);

				foreach (var method in methods) {
					if (method.GetParameters().Any() || method.IsGenericMethod || method.ReturnType == typeof(void)) {
						continue;
					}
					if (port.fieldName.Equals(method.Name, System.StringComparison.Ordinal)) {
						return method.Invoke(ingredient, null);
					}
				}
			}

			return base.GetValue(context, port);
		}
	}
}