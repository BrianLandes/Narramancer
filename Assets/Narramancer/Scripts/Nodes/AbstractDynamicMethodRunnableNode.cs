
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using XNode;

namespace Narramancer {

	[NodeWidth(500)]
	public abstract class AbstractDynamicMethodRunnableNode : ChainedRunnableNode {

		private static readonly string outputPortLabel = "Output Result";

		[SerializeField]
		protected SerializableMethod method = new SerializableMethod();

		[NonSerialized]
		private Dictionary<string, object> cachedOutResults = new Dictionary<string, object>();

		[NonSerialized]
		private object cachedResult;

		protected abstract object GetTargetObject(INodeContext context);

		protected override void Init() {
			method.OnChanged -= RebuildPorts;
			method.OnChanged += RebuildPorts;
		}

		protected virtual void RebuildPorts() {

			if (!method.IsValid()) {
				ClearDynamicPorts();
				return;
			}

			UpdateNodeName();

			var existingPorts = CreateParameterInputs().ToList();

			this.ClearDynamicPortsExcept(existingPorts);
		}

		protected void UpdateNodeName() {
			name = this.GetType().Name;
			name = name.Replace("Node", "");
			name = name.Replace("Method", method.MethodName);
			name = name.Nicify();
		}

		protected IEnumerable<NodePort> CreateParameterInputs() {

			var firstInput = true;

			foreach (var parameter in method.GetNonOutParameters()) {

				// Handle Cancellation Tokens outside of ports
				if (parameter.Type.IsAssignableFrom(typeof(CancellationToken))) {
					continue;
				}

				if (method.IsExtension && firstInput) {
					firstInput = false;
					continue;
				}

				yield return this.GetOrAddDynamicInput(parameter.Type, parameter.name);

			}

			if (ShouldAddReturnOutputPort()) {
				yield return this.GetOrAddDynamicOutput(method.ReturnType, outputPortLabel);
			}


			foreach (var parameter in method.GetOutParameters()) {
				yield return this.GetOrAddDynamicOutput(parameter.Type, parameter.name);
			}
		}

		private bool ShouldAddReturnOutputPort() {
			if (method.ReturnType == null) {
				return false;
			}
			if (typeof(void).IsAssignableFrom(method.ReturnType)) {
				return false;
			}
			if (method.IsReturnType<Promise>()) {
				return false;
			}
			return true;
		}

		public override void Run(NodeRunner runner) {
			base.Run(runner);

			if (!method.IsValid()) {
				return;
			}

			object[] parameters = GetInputParameters(runner.Blackboard);

			object targetObject = GetTargetObject(runner.Blackboard);

			string callerDescription = $"{this.name} within {graph.name}, attempting to call {method}";

			try {

				var result = method.Invoke(targetObject, parameters, callerDescription);
				CacheResults(result, parameters);
			}
			catch( Exception e ) {
				Debug.LogException(e, this);
			}

			
		}

		private object[] GetInputParameters(INodeContext context) {
			if (!method.IsValid()) {
				return null;
			}

			var result = new List<object>();
			var firstInput = true;
			foreach (var parameter in method.GetAllParameters()) {
				if (parameter.isOut) {

					result.Add(null); // null tells the array empty space
				}
				else
				if (method.IsExtension && firstInput) {
					firstInput = false;
					result.Add(GetTargetObject(context));
				}
				else {
					var inputPort = GetPort(parameter.name);

					var inputFromPort = inputPort.GetInputValue(context);

					result.Add(inputFromPort);
				}
			}

			return result.ToArray();
		}

		private void CacheResults(object outputResult, object[] parametersArray) {
			// in output result and any 'out' parameters

			cachedResult = outputResult;

			for (int ii = 0; ii < parametersArray.Length; ii++) {
				var parameterField = method.Parameters[ii];
				if (parameterField.isOut) {

					var parameterResult = parametersArray[ii];

					cachedOutResults[parameterField.name] = parameterResult;

				}
			}


		}

		public override object GetValue(INodeContext context, NodePort port) {

			if (!method.IsValid()) {
				return base.GetValue(context, port);
			}

			if (port.fieldName.Equals(outputPortLabel)) {
				return cachedResult;
			}

			foreach (var parameter in method.GetOutParameters()) {
				if (port.fieldName.Equals(parameter.name)) {
					if (cachedOutResults.TryGetValue(parameter.name, out var result)) {
						return result;
					}
					return null;
				}
			}

			return base.GetValue(context, port);
		}

	}
}