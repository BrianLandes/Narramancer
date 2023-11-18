
using System;
using System.Reflection;
using UnityEngine;

namespace Narramancer {

	[Serializable]
	public class SerializableParameter {

		#region Parameter Description

		public string name;

		public string typeName;

		public string assemblyQualifiedName;

		public bool isOut;

		#endregion

		#region Nonserialized / Editor Fields

		[NonSerialized]
		private ParameterInfo parameterInfo;

		[NonSerialized]
		private Type cachedType;

		//[NonSerialized]
		//private object cachedResult;

		#endregion

		#region Properties

		public Type Type {
			get {
				if (cachedType == null) {
					cachedType = AssemblyUtilities.GetType(assemblyQualifiedName);
					if (cachedType != null) {
						assemblyQualifiedName = cachedType.AssemblyQualifiedName;
					}
				}
				return cachedType;
			}
		}

		#endregion

		public SerializableParameter(ParameterInfo parameterInfo) {

			this.parameterInfo = parameterInfo;

			name = parameterInfo.Name;
			var parameterType = parameterInfo.ParameterType;
			assemblyQualifiedName = parameterType.AssemblyQualifiedName;
			typeName = parameterType.Name;

			//typeName = parameterInfo.ParameterType.FullName.TrimEnd('&');

			isOut = parameterInfo.IsOut;
		}
	}
}