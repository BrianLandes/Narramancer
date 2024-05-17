using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using XNode;

namespace Narramancer {

	[CreateNodeMenu("Verb/Run Noun Ingredient Value Verb")]
	public class RunNounIngredientValueVerbNode : AbstractInstanceInputNode {

		[SerializeField]
		[HideInInspector] // handled in RunNounIngredientActionVerbNodeEditor.cs
		private SerializableType ingredientType = new SerializableType();
		public static string IngredientTypeFieldName => nameof(ingredientType);
		public Type IngredientType => ingredientType.Type;

		[SerializeField]
		[HideInInspector] // handled in RunNounIngredientActionVerbNodeEditor.cs
		private string verbName = string.Empty;
		public static string VerbNameFieldName => nameof(verbName);
		public FieldInfo VerbFieldInfo => IngredientType?.GetField(verbName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy | BindingFlags.DeclaredOnly);

		public const string INSTANCE_INPUT = "instance";

		[SerializeField]
		[Output(ShowBackingValue.Never, ConnectionType.Multiple, TypeConstraint.Inherited)]
		private bool hasIngredient = false;


		protected override void Init() {
			ingredientType.OnChanged -= UpdatePorts;
			ingredientType.OnChanged += UpdatePorts;

			ingredientType.canBeList = false;

			ingredientType.typeFilter = type => typeof(AbstractNounIngredient).IsAssignableFrom(type);

		}

		public override void UpdatePorts() {
			if (ingredientType.Type == null) {
				ClearDynamicPorts();
			}
			else {
				var keepPorts = new List<NodePort>();

				var fieldInfo = VerbFieldInfo;
				if (fieldInfo != null) {
					foreach (var attribute in fieldInfo.GetCustomAttributes(false)) {

						if (attribute is RequireInputAttribute requireInputAttribute && requireInputAttribute.RequiredType != null) {
							var name = requireInputAttribute.DefaultName.IsNotNullOrEmpty() ? requireInputAttribute.DefaultName : requireInputAttribute.RequiredType.Name;
							if (name.Equals(INSTANCE_INPUT)) {
								// treat input ports with the name 'instance' special -> inject the given NounInstance at runtime
								continue;
							}
							var inputPort = this.GetOrAddDynamicInput(requireInputAttribute.RequiredType, name);
							keepPorts.Add(inputPort);
						}
						else
						if (attribute is RequireOutputAttribute requireOutputAttribute && requireOutputAttribute.RequiredType != null) {
							var name = requireOutputAttribute.DefaultName.IsNotNullOrEmpty() ? requireOutputAttribute.DefaultName : requireOutputAttribute.RequiredType.Name;
							var outputPort = this.GetOrAddDynamicOutput(requireOutputAttribute.RequiredType, name);
							keepPorts.Add(outputPort);
						}

					}
				}

				this.ClearDynamicPortsExcept(keepPorts);
			}

			base.UpdatePorts();
		}


		public override object GetValue(INodeContext context, NodePort port) {
			if (port.fieldName.Equals(nameof(passThroughInstance))) {
				return base.GetValue(context, port);
			}
			var instance = GetInstance(context);
			var ingredient = instance?.GetIngredient(ingredientType.Type);
			if (port.fieldName.Equals(nameof(hasIngredient))) {
				return ingredient !=null;
			}
			if (ingredient != null) {
				var verbFieldInfo = VerbFieldInfo;
				if (verbFieldInfo != null) {
					var valueVerb = verbFieldInfo.GetValue(ingredient) as ValueVerb;
					if (valueVerb != null) {
						NarramancerPort GetCorrespondingVerbPort(Type type, string name) {
							return valueVerb.Inputs.FirstOrDefault(x => x.Type == type && x.Name.Equals(name));
						}

						try {

							if (valueVerb.TryGetOutputNode(out var outputNode)) {

								var instanceInputPort = GetCorrespondingVerbPort(typeof(NounInstance), INSTANCE_INPUT);
								if (instanceInputPort != null) {
									var blackboard = context as Blackboard;
									blackboard.Set(instanceInputPort.VariableKey, instance);
								}

								foreach (var inputPort in DynamicInputs) {
									var verbPort = GetCorrespondingVerbPort(inputPort.ValueType, inputPort.fieldName);
									verbPort.AssignValueFromNodePort(context, inputPort);
								}

								foreach (var outputPort in valueVerb.Outputs) {

									if (port.fieldName.Equals(outputPort.Name)) {

										return outputNode.GetValue(context, outputPort);
									}
								}

								foreach (var inputPort in valueVerb.Inputs) {

									if (inputPort.PassThrough && port.fieldName.StartsWith(inputPort.Name)) {
										var nodePort = DynamicInputs.FirstOrDefault(xnodePort => xnodePort.fieldName.Equals(inputPort.Name));
										if (nodePort != null) {
											return nodePort.GetInputValue(context);
										}
										break;
									}
								}
							}
							else {
								Debug.LogError($"{nameof(ValueVerb).Nicify()} does not have an {nameof(OutputNode).Nicify()}.", valueVerb);
							}
						}
						catch (Exception e) {
							Debug.LogError($"Exception when Getting Value for '{valueVerb.name}' ('{graph.name}'): {e.Message}", valueVerb);
							throw;
						}

					}
				}
			}
			return base.GetValue(context, port);
		}

	}
}

