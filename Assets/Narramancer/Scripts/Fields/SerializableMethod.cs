
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Narramancer {

	/// <summary>
	/// A wrapper that allows (almost) any C# method to be serialized and displayed in Unity Inspector.
	/// </summary>
	[Serializable]
	public class SerializableMethod {

		[SerializeField]
		private string className;
		public string ClassName => className;

		[SerializeField]
		private string methodName;
		public string MethodName => methodName;

		[SerializeField]
		private string returnTypeName;

		[SerializeField]
		private string targetTypeName;

		[SerializeField]
		private SerializableParameter[] parameters;
		public SerializableParameter[] Parameters => parameters;

		[SerializeField]
		private bool isExtension;
		public bool IsExtension => isExtension;

		[SerializeField]
		private string assemblyName;

		[NonSerialized]
		private Type cachedReturnType;

		[NonSerialized]
		private Type cachedTargetType;

		public Type ReturnType { 
			get {
				if (cachedReturnType == null) {
					cachedReturnType = AssemblyUtilities.GetType(returnTypeName);
					if ( cachedReturnType != null) {
						returnTypeName = cachedReturnType.AssemblyQualifiedName;
					}
				}
				return cachedReturnType;
			}
		}

		public Type TargetType {
			get {
				if (cachedTargetType == null) {
					cachedTargetType = AssemblyUtilities.GetType(targetTypeName);
					if (cachedTargetType != null) {
						targetTypeName = cachedTargetType.AssemblyQualifiedName;
					}
				}
				return cachedTargetType;
			}
		}

		[NonSerialized]
		private MethodInfo methodInfo;

		/// <summary>
		/// Any type(s) to use when looking up methods. Used by the drawer.
		/// </summary>
		public Type[] LookupTypes { get; set; } = null;
		public Func<Type[]> GetLookupTypes { get; set; }

		/// <summary>
		/// Allows Nodes and other GUIs to respond to the type being changed.
		/// </summary>
		public event Action OnChanged;

		public SerializableMethod() { 
		}

		public SerializableMethod(MethodInfo method) {
			SetTargetMethod(method);
		}

		public void SetTargetMethod(MethodInfo method) {
			Type targetType = method.ReflectedType;
			className = targetType.Name;
			targetTypeName = targetType.AssemblyQualifiedName;
			cachedTargetType = targetType;

			methodName = method.Name;

			returnTypeName = method.ReturnType.AssemblyQualifiedName;
			cachedReturnType = method.ReturnType;

			isExtension = method.IsDefined(typeof(ExtensionAttribute), false);

			assemblyName = method.DeclaringType.Assembly.FullName;

			parameters = method.GetParameters().Select(parameter => new SerializableParameter(parameter)).ToArray();

			methodInfo = method;
		}

		public void Clear() {
			className = string.Empty;
			targetTypeName = string.Empty;
			cachedTargetType = null;
			methodName = string.Empty;
			returnTypeName = string.Empty;
			cachedReturnType = null;
			isExtension = false;
			assemblyName = string.Empty;
			parameters = null;
			methodInfo = null;
		}

		public void ApplyChanges() {
			cachedReturnType = null;
			cachedTargetType = null;
			OnChanged?.Invoke();
		}
		public override string ToString() {
			if (className.IsNullOrEmpty() || methodName.IsNullOrEmpty()) {
				return "(None)";
			}
			if (parameters == null || parameters.Length == 0) {
				return $"{className}.{methodName}()";
			}
			var parameterString = parameters.Select(parameter => parameter.typeName).CommaSeparated();
			return $"{className}.{methodName}({parameterString})";
		}

		public string ToolTip() {
			if (className.IsNullOrEmpty() || methodName.IsNullOrEmpty()) {
				return "(None)";
			}
			if (parameters == null || parameters.Length == 0) {
				return $"{methodName}()";
			}
			var parameterString = parameters.Select(parameter => parameter.typeName).CommaSeparated();
			return $"{methodName}({parameterString})";
		}

		public object Invoke(object target, object[] parameters, string callerDescription = "") {

			var type = TargetType;
			var parameterTypes = GetParameterTypes();
			methodInfo = methodInfo!=null ? methodInfo : type.GetMethod(methodName, parameterTypes);

			if (methodInfo == null) {
				var parameterList = new List<Type>(parameterTypes);
				parameterList.RemoveAt(0);
				parameterTypes = parameterList.ToArray();
				if (assemblyName.IsNotNullOrEmpty()) {
					var assembly = AssemblyUtilities.GetAssembly(assemblyName);
					methodInfo = type.GetExtensionMethod(assembly, methodName, parameterTypes);
				}
				else {
					methodInfo = type.GetExtensionMethod(methodName, parameterTypes);
				}
			}

			if (methodInfo == null) {
				Debug.LogError($"Could not find a valid method for {callerDescription} with name {methodName}");
				return null;
			}

			try {

				return methodInfo.Invoke(target, parameters);
			}
			catch (Exception e) {
				Debug.LogError($"Invoking method for {callerDescription} threw the error");
				throw e;
			}
		}


		#region Parameters

		public Type[] GetParameterTypes() {
			return parameters?.Select(parameter => parameter.Type).ToArray();
		}

		public IEnumerable<SerializableParameter> GetAllParameters() {
			return parameters;
		}

		public IEnumerable<SerializableParameter> GetOutParameters() {
			return parameters.Where(parameter => parameter.isOut);
		}

		public IEnumerable<SerializableParameter> GetNonOutParameters() {
			return parameters.Where(parameter => !parameter.isOut);
		}

		public IEnumerable<SerializableParameter> GetNonOutParametersWithType<T>() {
			return parameters.Where(parameter => !parameter.isOut && parameter.Type.IsAssignableFrom(typeof(T)));
		}

		#endregion

		#region Return

		public bool IsReturnType<T>() {
			return typeof(T).IsAssignableFrom(ReturnType);
		}

		#endregion

	}

	public static class SerializableMethodExtensions {
		public static bool IsValid(this SerializableMethod method) {
			if (method == null) {
				return false;
			}
			if (method.ClassName.IsNullOrEmpty()) {
				return false;
			}
			if (method.MethodName.IsNullOrEmpty()) {
				return false;
			}
			return true;
		}

	}
}
