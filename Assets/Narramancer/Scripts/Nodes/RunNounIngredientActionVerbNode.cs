using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using XNode;

namespace Narramancer {

	[NodeWidth(350)]
	[CreateNodeMenu("Verb/Run Noun Ingredient Action Verb")]
	public class RunNounIngredientActionVerbNode : AbstractInstanceInputChainedRunnableNode {

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

		protected override void Init() {
			ingredientType.OnChanged -= UpdatePorts;
			ingredientType.OnChanged += UpdatePorts;

			ingredientType.canBeList = false;

			ingredientType.typeFilter = type => typeof(AbstractNounIngredient).IsAssignableFrom(type);

		}

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			var instance = GetInstance(runner.Blackboard);
			var ingredient = instance?.GetIngredient(ingredientType.Type);
			if (ingredient == null) {
				return;
			}
			var verbFieldInfo = VerbFieldInfo;
			if (verbFieldInfo == null) {
				return;
			}

			var actionVerb = verbFieldInfo.GetValue(ingredient) as ActionVerb;
			if (actionVerb == null) {
				return;
			}

			if ( actionVerb.TryGetFirstRunnableNodeAfterRootNode(out var runnableNode)) {

				NarramancerPort GetCorrespondingRunnableGraphPort(Type type, string name) {
					return actionVerb.Inputs.FirstOrDefault(x => x.Type == type && x.Name.Equals(name));
				}

				var instanceInputPort = GetCorrespondingRunnableGraphPort(typeof(NounInstance), INSTANCE_INPUT);
				if (instanceInputPort != null) {
					runner.Blackboard.Set(instanceInputPort.VariableKey, instance);
				}
				
				foreach (var inputPort in DynamicInputs) {
					try {
						var runnableGraphPort = GetCorrespondingRunnableGraphPort(inputPort.ValueType, inputPort.fieldName);
						if (runnableGraphPort!=null) {
							runnableGraphPort.AssignValueFromNodePort(runner.Blackboard, inputPort);
						}
						
					}
					catch (Exception e) {
						Debug.LogError($"Exception during AssignGraphVariableInputs for NodePort '{inputPort.fieldName}', RunnableGraph: '{actionVerb.name}', Within Graph: '{graph.name}': {e.Message}", actionVerb);
						throw;
					}
				}

				runner.Prepend(runnableNode);
			}
			else {
				Debug.LogError("Runnable Graph missing Root Node", actionVerb);
			}
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
			if (ingredient != null) {
				var verbFieldInfo = VerbFieldInfo;
				if (verbFieldInfo != null) {
					var actionVerb = verbFieldInfo.GetValue(ingredient) as ActionVerb;
					if (actionVerb != null) {
						foreach (var outputPort in actionVerb.Outputs) {

							if (port.fieldName.Equals(outputPort.Name)) {
								var blackboard = context as Blackboard;
								var value = blackboard.Get(outputPort.VariableKey, outputPort.Type);
								return value;
							}
						}

						foreach (var inputPort in actionVerb.Inputs) {

							if (inputPort.PassThrough && port.fieldName.StartsWith(inputPort.Name)) {
								var nodePort = DynamicInputs.FirstOrDefault(xnodePort => xnodePort.fieldName.Equals(inputPort.Name));
								if (nodePort != null) {
									return nodePort.GetInputValue(context);
								}
								break;
							}
						}
					}
				}
			}
			return base.GetValue(context, port);
		}

	}
}

