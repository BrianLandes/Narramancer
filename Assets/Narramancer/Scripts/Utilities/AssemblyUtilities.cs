
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Narramancer {

	public static class AssemblyUtilities {

		public static IEnumerable<Assembly> GetAllAssemblies(bool excludeDynamic = true, bool excludeSystem = false, bool excludeUnity = false) {

			return AppDomain.CurrentDomain.GetAssemblies().Where(a =>
					(!excludeDynamic || !a.IsDynamic) &&
					(!excludeSystem || !a.GlobalAssemblyCache && !a.FullName.StartsWith("System") && !a.FullName.StartsWith("mscorlib") && !a.FullName.StartsWith("netstandard")) &&
					(!excludeUnity || !a.FullName.StartsWith("UnityEditor") && !a.FullName.StartsWith("UnityEngine") && !a.FullName.StartsWith("Unity.") &&
						!a.FullName.StartsWith("nunit.") && !a.FullName.StartsWith("ExCSS.") && !a.FullName.StartsWith("UniTask.") &&
						!a.FullName.StartsWith("UniRx.") && !a.FullName.StartsWith("JetBrains.") && !a.FullName.StartsWith("Newtonsoft."))
				);
			;
		}

		public static Assembly GetAssembly(string assembly) {
			return Assembly.Load(assembly);
		}

		public static IEnumerable<Type> GetAllTypes(bool excludeDynamic = true, bool excludeSystem = false, bool excludeUnity = false) {
			return GetAllAssemblies(excludeDynamic, excludeSystem, excludeUnity)
				.SelectMany(assembly => assembly.GetTypes())
				.Where(x => !x.IsAbstract)
				.Where(x => !x.IsGenericTypeDefinition);
		}

		public static IEnumerable<Type> GetAllPublicTypes(bool excludeDynamic = true, bool excludeSystem = false, bool excludeUnity = false) {
			return GetAllAssemblies(excludeDynamic, excludeSystem, excludeUnity)
				.SelectMany(assembly => assembly.GetTypes())
				.Where(x => !x.IsAbstract)
				.Where(x => x.IsPublic)
				.Where(x => !x.IsGenericTypeDefinition);
		}

		public static IEnumerable<Type> GetAllTypes<T>() {
			return GetAllTypes()
				.Where(x => typeof(T).IsAssignableFrom(x));
		}

		public static IEnumerable<Type> GetAllTypes(Type type) {
			return GetAllTypes()
				.Where(x => type.IsAssignableFrom(x));
		}

		public static IEnumerable<Type> GetAllNonObsoleteTypes<T>() {
			return GetAllTypes<T>()
				.Where(x => !x.IsDefined(typeof(ObsoleteAttribute), true)); // Skip types with the 'Obsolete' attribute;
		}

		public static IEnumerable<Type> GetAllNonObsoleteTypes(Type type) {
			return GetAllTypes(type)
				.Where(x => !x.IsDefined(typeof(ObsoleteAttribute), true)); // Skip types with the 'Obsolete' attribute;
		}

		public static IEnumerable<Type> GetAllTypesInNamespace(string onlyIncludeNamespace) {
			return GetAllTypes().Where(x => x.Namespace.Equals(onlyIncludeNamespace));
		}

		public static IEnumerable<Type> GetAllTypesInNamespace<T>(string onlyIncludeNamespace) {
			return GetAllTypes<T>().Where(x => x.Namespace.Equals(onlyIncludeNamespace));
		}

		public static IEnumerable<Type> GetAllNonObsoleteTypesInNamespace<T>(string onlyIncludeNamespace) {
			return GetAllNonObsoleteTypes<T>().Where(x => x.Namespace.Equals(onlyIncludeNamespace));
		}

		public static IEnumerable<Type> GetAllStaticTypes(bool excludeDynamic = true, bool excludeSystem = false, bool excludeUnity = false) {
			return GetAllAssemblies(excludeDynamic, excludeSystem, excludeUnity)
				.SelectMany(assembly => assembly.GetTypes())
				.Where(type => type.IsAbstract && type.IsSealed); // 'static' classes are simply abstract
		}

		public static IEnumerable<Type> GetAllStaticTypesWithAttribute<T>() where T : Attribute {
			return GetAllStaticTypes()
				.Where(type => type.IsDefined(typeof(T), true));
		}

		public static Type GetStaticTypeWithName(string typeName) {
			return GetAllStaticTypes().Where(type => type.Name.Equals(typeName)).FirstOrDefault();
		}

		public static IEnumerable<MethodInfo> GetExtensionMethods(this Type type) {
			return GetAllAssemblies().SelectMany(assembly => type.GetExtensionMethods(assembly));
		}

		public static IEnumerable<MethodInfo> GetExtensionMethods(this Type type, Assembly extensionsAssembly) {
			var query = from t in extensionsAssembly.GetTypes()
						where !t.IsGenericType && !t.IsNested
						from m in t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
						where m.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute), false)
						where m.GetParameters()[0].ParameterType == type
						select m;

			return query;
		}

		public static MethodInfo GetExtensionMethod(this Type type, string name) {
			return GetAllAssemblies().Select(assembly => type.GetExtensionMethod(assembly, name)).FirstOrDefault(m => m != null);
		}

		public static MethodInfo GetExtensionMethod(this Type type, Assembly extensionsAssembly, string name) {
			return type.GetExtensionMethods(extensionsAssembly).FirstOrDefault(m => m.Name == name);
		}

		public static MethodInfo GetExtensionMethod(this Type type, Assembly extensionsAssembly, string name, Type[] types) {

			var methods = type.GetExtensionMethods(extensionsAssembly)
				.Where(method => method.Name == name &&
				method.GetParameters().Count() == types.Length + 1) // + 1 because extension method parameter (this)
				.ToList();

			if (!methods.Any()) {
				return default(MethodInfo);
			}

			if (methods.Count() == 1) {
				return methods.First();
			}

			var methodInfo = methods.FirstOrDefault(method => {
				var parameters = method.GetParameters();
				for (int ii = 1; ii <= types.Length; ii++) {
					int parametersIndex = parameters.Length - ii;
					var parameterType = parameters[parametersIndex].ParameterType;
					int typesIndex = types.Length - ii;
					var targetType = types[typesIndex];
					if (!parameterType.FullName.Equals(targetType.FullName)) {
						return false;
					}
				}
				return true;
			});

			if (methodInfo != null) {
				return methodInfo;
			}

			return default(MethodInfo);
		}

		public static MethodInfo GetExtensionMethod(this Type type, string name, Type[] types) {
			return GetAllAssemblies().Select(assembly => type.GetExtensionMethod(assembly, name, types)).FirstOrDefault(method => method != null);
		}


		public static bool IsListOfIdentifiables(this Type type) {
			if (typeof(IList).IsAssignableFrom(type)) {
				return true;
			}
			foreach (var it in type.GetInterfaces()) {
				if (it.IsGenericType && typeof(IList<>) == it.GetGenericTypeDefinition()) {
					return true;
				}
			}
			return false;
		}

		public static bool IsListType(Type type) {
			if (typeof(IList).IsAssignableFrom(type)) {
				return true;
			}
			foreach (var it in type.GetInterfaces()) {
				if (it.IsGenericType && typeof(IList<>) == it.GetGenericTypeDefinition()) {
					return true;
				}
			}
			return false;
		}

		public static bool IsListTypeOfType(Type type, Type listType) {
			if (!IsListType(type)) {
				return false;
			}
			if (type.GenericTypeArguments.Length > 0) {
				if (type.GenericTypeArguments[0] != listType) {
					return false;
				}
			}
			else
				if (type.GetElementType() != listType) {
				return false;
			}
			return true;
		}

		public static Type GetListInnerType(Type listType) {
			if (!IsListType(listType)) {
				return null;
			}
			if (listType.GenericTypeArguments.Length > 0) {
				return listType.GenericTypeArguments[0];
			}
			else{
				return listType.GetElementType();
			}
		}

		public static bool HasFieldWithType(this Type classType, Type fieldType) {
			var fields = classType.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (fields.Any(field => field.FieldType == fieldType)) {
				return true;
			}
			if (fields.Any(field => IsListTypeOfType(field.FieldType, fieldType))) {
				return true;
			}
			return false;
		}

		public static bool FieldHasValue(this object target, Type fieldType, object value) {
			var classType = target.GetType();
			var fields = classType.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			var field = fields.FirstOrDefault(x => x.FieldType == fieldType);
			if (field != null) {
				var fieldValue = field.GetValue(target);
				return fieldValue == value;
			}

			var listField = fields.FirstOrDefault(x => IsListTypeOfType(x.FieldType, fieldType));
			if (listField != null) {
				var fieldValue = listField.GetValue(target);
				var objectList = ToListOfObjects(fieldValue);

				return objectList.Contains(value);
			}
			return false;
		}

		public static bool IsAssignableFrom(this Type type, params Type[] types) {
			foreach (var t in types) {
				if (type.IsAssignableFrom(t)) {
					return true;
				}
			}
			return false;
		}

		public static List<object> ToListOfObjects(object listObject) {
			if (listObject == null) {
				return null;
			}
			var resultList = listObject as List<object>;
			if (resultList != null) {
				return resultList;
			}

			var type = listObject.GetType();

			var toArrayMethod = type.GetMethod("ToArray");

			var arrayAsObject = toArrayMethod.Invoke(listObject, null);
			var objectArray = arrayAsObject as object[];
			if (objectArray != null) {
				return objectArray.ToList();
			}

			var array = arrayAsObject as Array;
			resultList = new List<object>();
			for (var ii = 0; ii < array.Length; ii++) {
				resultList.Add(array.GetValue(ii));
			}
			return resultList;
		}

		private static Regex typeNameRegex = new Regex(@"([a-zA-Z_\-0-9]*)\.([a-zA-Z_\-0-9]*), ([\.a-zA-Z_-]*), Version[=0-9\.]*");

		public static Type GetType(string assemblyQualifiedName) {
			if (assemblyQualifiedName.IsNullOrEmpty()) {
				Debug.LogError("Type name was null or empty");
				return null;
			}
			var result = Type.GetType(assemblyQualifiedName);
			if (result != null) {
				return result;
			}
#if UNITY_EDITOR
			// Perform some fuzzy search as a fail safe (scripts may have moved or changed assemblies)
			var match = typeNameRegex.Match(assemblyQualifiedName);

			var matchedNamespace = match.Groups[1].Value;
			var className = match.Groups[2].Value;
			var assembly = match.Groups[3].Value;

			var allTypes = GetAllTypes().ToList();

			result = GetAllTypes().FirstOrDefault(type => (type.Namespace.IsNullOrEmpty() || type.Namespace.Equals(matchedNamespace)) && type.Name.Equals(className));
			if (result != null) {
				return result;
			}
#endif
			Debug.LogError("Could not find a type with the name " + assemblyQualifiedName);
			return null;
		}

		public static string GetClassName(string assemblyQualifiedName) {
			var match = typeNameRegex.Match(assemblyQualifiedName);
			var className = match.Groups[2].Value;
			return className;
		}
	}
}