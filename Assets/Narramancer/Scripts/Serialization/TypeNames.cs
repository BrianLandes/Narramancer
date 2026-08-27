using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Narramancer.Serialization {

	/// <summary>
	/// Names a <see cref="Type"/> for storage in a save, and resolves that name back to the live type.
	/// <para>
	/// The stored form is the assembly-qualified name with <c>Version</c>, <c>Culture</c> and
	/// <c>PublicKeyToken</c> stripped — including from the arguments of a generic type, recursively. A
	/// version bump would otherwise invalidate every type tag in every existing save, and Unity bumps
	/// assembly versions for reasons that have nothing to do with the plugin.
	/// </para>
	/// <para>
	/// Resolution is deliberately forgiving about <em>where</em> a type lives and strict about
	/// <em>whether</em> it was found. A script that moved between assemblies still resolves, via a
	/// full-name scan across everything loaded; a type that no longer exists throws rather than
	/// resolving to a plausible neighbour, because a save that refuses to load beats one that loads
	/// wrong. That distinction is not academic — see the instanceID finding in
	/// docs/ODIN_REMOVAL_PLAN.md, where a silently-wrong reference produced no errors at all.
	/// </para>
	/// </summary>
	public static class TypeNames {

		private static readonly Regex assemblyDecorations =
			new Regex(@",\s*(Version|Culture|PublicKeyToken)=[^,\]]+", RegexOptions.Compiled);

		private static readonly Dictionary<Type, string> writeCache = new Dictionary<Type, string>();
		private static readonly Dictionary<string, Type> readCache = new Dictionary<string, Type>(StringComparer.Ordinal);

		// Built lazily on the first name that does not resolve directly, then reused: the scan over every
		// loaded assembly is the expensive step and the answer does not change within a session.
		private static Dictionary<string, Type> typesByFullName;

		/// <summary>The stored name for a type. Never throws — any type can be named.</summary>
		public static string Write(Type type) {
			if (type == null) {
				throw new ArgumentNullException(nameof(type));
			}
			if (writeCache.TryGetValue(type, out var cached)) {
				return cached;
			}

			var qualified = type.AssemblyQualifiedName ?? type.FullName ?? type.Name;
			var name = assemblyDecorations.Replace(qualified, string.Empty);
			writeCache[type] = name;
			return name;
		}

		/// <summary>The type for a stored name, or a <see cref="SerializationException"/> naming what was missing.</summary>
		public static Type Read(string name) {
			if (TryRead(name, out var type)) {
				return type;
			}
			throw new SerializationException($"Could not resolve the type '{name}'. It may have been renamed or removed since the save was written.");
		}

		/// <summary>Non-throwing <see cref="Read"/>: false (and null) when the name resolves nowhere.</summary>
		public static bool TryRead(string name, out Type type) {
			if (string.IsNullOrEmpty(name)) {
				type = null;
				return false;
			}
			if (readCache.TryGetValue(name, out type)) {
				return type != null;
			}

			type = Resolve(name);
			readCache[name] = type;
			return type != null;
		}

		private static Type Resolve(string name) {
			// Type.GetType applies these two callbacks to the outer type *and* to each generic argument,
			// so a name like "List`1[[Foo, Narramancer]], mscorlib" resolves piece by piece with the same
			// version-independent rules at every level.
			return Type.GetType(name, ResolveAssembly, ResolveTypeInAssembly, throwOnError: false);
		}

		private static Assembly ResolveAssembly(AssemblyName assemblyName) {
			try {
				// Name only: a stored name carries no version, and we do not want one honoured anyway.
				return Assembly.Load(new AssemblyName(assemblyName.Name));
			}
			catch (Exception) {
				foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
					if (string.Equals(assembly.GetName().Name, assemblyName.Name, StringComparison.Ordinal)) {
						return assembly;
					}
				}

				// Returning null hands the type name to ResolveTypeInAssembly with a null assembly, which
				// falls through to the cross-assembly scan — this is how a script that moved assemblies
				// still resolves.
				return null;
			}
		}

		private static Type ResolveTypeInAssembly(Assembly assembly, string typeName, bool ignoreCase) {
			if (assembly != null) {
				var type = assembly.GetType(typeName, throwOnError: false, ignoreCase: ignoreCase);
				if (type != null) {
					return type;
				}
			}
			return FindByFullName(typeName);
		}

		private static Type FindByFullName(string fullName) {
			EnsureIndex();
			return typesByFullName.TryGetValue(fullName, out var type) ? type : null;
		}

		private static void EnsureIndex() {
			if (typesByFullName != null) {
				return;
			}

			var index = new Dictionary<string, Type>(StringComparer.Ordinal);
			foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
				Type[] types;
				try {
					types = assembly.GetTypes();
				}
				catch (ReflectionTypeLoadException exception) {
					// A partially-loadable assembly still contributes the types that did load.
					types = exception.Types;
				}
				catch (Exception) {
					continue;
				}

				foreach (var type in types) {
					var fullName = type?.FullName;
					if (fullName != null && !index.ContainsKey(fullName)) {
						index[fullName] = type;
					}
				}
			}

			typesByFullName = index;
		}

		/// <summary>Drops the cached lookups. Tests only — nothing changes types at runtime.</summary>
		internal static void ClearCaches() {
			writeCache.Clear();
			readCache.Clear();
			typesByFullName = null;
		}
	}
}
