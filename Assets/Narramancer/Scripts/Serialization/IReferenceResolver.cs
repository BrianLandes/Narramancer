using System;

namespace Narramancer.Serialization {

	/// <summary>
	/// The bridge between a stable id and the asset a saved value points at. Everything a save refers
	/// to but does not own — node assets, <c>NounScriptableObject</c>s, sprites — is written as an id
	/// handle rather than inlined, and resolved back through this seam on load.
	/// <para>
	/// The id must be stable <em>across builds</em>, which is the whole reason this interface exists.
	/// The Odin path stored <c>instanceID</c>s, which are handed out by a session-global counter: a
	/// patched build renumbers them and every reference in every existing save then points at whatever
	/// happens to sit at that number. That failure was reproduced and is documented in
	/// docs/ODIN_REMOVAL_PLAN.md — it produced no errors, no nulls, and a story that silently resumed at
	/// the wrong node. The Unity implementation keys on the asset GUID instead.
	/// </para>
	/// <para>
	/// Both methods are total: they return <c>false</c> rather than throwing. Deciding what an
	/// unresolvable reference means is the serializer's call, not the resolver's.
	/// </para>
	/// </summary>
	public interface IReferenceResolver {

		/// <summary>
		/// Resolve an id to the asset it names, requiring the result to be assignable to
		/// <paramref name="type"/> (pass <c>null</c> to accept anything). Returns <c>false</c> when the
		/// id is unknown or the asset is of the wrong type.
		/// </summary>
		bool TryResolve(string id, Type type, out UnityEngine.Object asset);

		/// <summary>The stable id for an asset, or <c>false</c> if this resolver does not know it.</summary>
		bool TryGetId(UnityEngine.Object asset, out string id);
	}
}
