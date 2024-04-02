using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Narramancer {

	[Serializable]
	public class NounInstance {

		[SerializeField]
		private string displayName;
		public string DisplayName { get => displayName; set => displayName = value; }

		[SerializeField]
		private NounUID uid;
		public NounUID UID => uid;

		[SerializeField]
		private NounScriptableObject noun;
		public NounScriptableObject Noun => noun;

		public bool HasNoun() => Noun != null;

		[SerializeField]
		private List<PropertyInstance> properties = new List<PropertyInstance>();

		[SerializeField]
		private List<StatInstance> stats = new List<StatInstance>();

		/// <summary> Any relationships that involve this instance. </summary>
		[SerializeField]
		private List<RelationshipInstance> relationships = new List<RelationshipInstance>();
		public List<RelationshipInstance> Relationships => relationships;

		public GameObject GameObject { get; set; }

		public bool HasGameObject => GameObject != null;

		[SerializeField]
		private Blackboard blackboard = new Blackboard();

		public Blackboard Blackboard => blackboard;

		#region Constructors

		public NounInstance(IInstancable instancable) {

			this.displayName = instancable.DisplayName;
			this.uid = instancable.ID;
			this.noun = instancable as NounScriptableObject;
			foreach (var assignment in instancable.Properties) {
				var property = assignment.property;
				AddProperty(property);
			}
			foreach (var stat in instancable.Stats) {
				var statInstance = new StatInstance(stat.stat, this) {
					Value = stat.value
				};
				stats.Add(statInstance);
			}
			if (instancable.Blackboard != null) {
				blackboard = instancable.Blackboard.Copy();
			}

		}

		#endregion

		#region Properties

		public PropertyInstance this[PropertyScriptableObject property] {
			get => GetProperty(property);
			set {
				RemoveProperty(property);
				AddProperty(value);
			}
		}

		public bool HasProperty(PropertyScriptableObject propertyObject) {
			return GetProperty(propertyObject) != null;
		}

		public void AddProperty(PropertyScriptableObject propertyObject) {
			if (!TryGetProperty(propertyObject, out var propertyInstance)) {
				propertyInstance = new PropertyInstance(propertyObject, this);
				AddProperty(propertyInstance);
			}
		}

		public void AddProperty(PropertyInstance propertyInstance) {
			properties.Add(propertyInstance);
			NarramancerSingleton.Instance.ClearQueryInstancesTable();
			foreach (var modifier in propertyInstance.Adjective.Modifiers) {
				modifier.OnAdded(propertyInstance, this);
			}
		}

		public bool TryGetProperty(PropertyScriptableObject propertyObject, out PropertyInstance propertyInstance) {
			propertyInstance = GetProperty(propertyObject);
			return propertyInstance != null;
		}

		public PropertyInstance GetProperty(PropertyScriptableObject propertyObject) {
			var instance = properties.FirstOrDefault(property => property.Adjective == propertyObject);
			return instance;
		}

		public void AddProperties(params PropertyScriptableObject[] properties) {
			foreach (var property in properties.WithoutNulls()) {
				AddProperty(property);
			}
		}

		public void AddProperties(IEnumerable<PropertyScriptableObject> properties) {
			foreach (var property in properties.WithoutNulls()) {
				AddProperty(property);
			}
		}

		public void RemoveProperty(PropertyScriptableObject propertyNoun) {
			if (TryGetProperty(propertyNoun, out var propertyInstance)) {
				RemoveProperty(propertyInstance);
			}
		}

		public void RemoveProperty(PropertyInstance propertyInstance) {
			properties.Remove(propertyInstance);
			NarramancerSingleton.Instance.ClearQueryInstancesTable();
			foreach (var modifier in propertyInstance.Adjective.Modifiers) {
				modifier.OnRemoved(propertyInstance, this);
			}
		}

		public void RemoveProperties(params PropertyScriptableObject[] properties) {
			foreach (var property in properties.WithoutNulls()) {
				RemoveProperty(property);
			}
		}

		public void RemoveProperties(IEnumerable<PropertyScriptableObject> properties) {
			foreach (var property in properties.WithoutNulls()) {
				RemoveProperty(property);
			}
		}

		public void RemoveProperties(params PropertyInstance[] properties) {
			foreach (var property in properties.WithoutNulls()) {
				RemoveProperty(property);
			}
		}

		public void RemoveProperties(IEnumerable<PropertyInstance> properties) {
			foreach (var property in properties.WithoutNulls()) {
				RemoveProperty(property);
			}
		}

		public List<PropertyInstance> GetAllProperties() => properties.ToList();


		public List<PropertyInstance> GetPropertiesWith<T>() where T : AbstractPropertyModifierIngredient {
			return properties.Where(property => property.HasModifier<T>()).ToList();
		}

		public List<PropertyInstance> GetPropertiesWith(Type type) {
			return properties.Where(property => property.HasModifier(type)).ToList();
		}

		#endregion

		#region String Values

		public override string ToString() {
			return Name();
		}

		public string Name() {
			if (displayName.IsNotNullOrEmpty()) {
				return displayName;
			}
			return base.ToString();
		}

		#endregion

		#region Stats

		public StatInstance InstantiateAndAddStat(StatScriptableObject statObject) {
			var statInstance = new StatInstance(statObject, this);
			stats.Add(statInstance);
			return statInstance;
		}

		public bool HasStat(StatScriptableObject statObject) {
			var instance = stats.FirstOrDefault(stat => stat.Adjective == statObject);
			return instance != null;
		}

		public StatInstance GetStatInstance(StatScriptableObject statObject) {
			var instance = stats.FirstOrDefault(stat => stat.Adjective == statObject);
			if (instance == null) {
				instance = InstantiateAndAddStat(statObject);
			}

			return instance;
		}

		public float GetStatEffectiveValue(object context, StatScriptableObject statObject) {
			var statInstance = GetStatInstance(statObject);

			return statInstance.GetEffectiveValue(this, context);
		}
		public List<StatInstance> GetAllStats() => stats.ToList();

		#endregion Stats

		#region Relationships

		public RelationshipInstance GetRelationship(RelationshipScriptableObject relationship, NounInstance other, RelationshipRequirement requirement = RelationshipRequirement.Source) {
			switch (requirement) {
				case RelationshipRequirement.Source:
					return relationships.FirstOrDefault(x => x.Involves(relationship, this, other));
				case RelationshipRequirement.Destination:
					return relationships.FirstOrDefault(x => x.Involves(relationship, other, this));
				case RelationshipRequirement.Either:
				default:
					return relationships.FirstOrDefault(x => x.Involves(relationship) && x.Involves(other) && x.Involves(this));
			}
		}

		public RelationshipInstance GetRelationship(RelationshipScriptableObject relationship, RelationshipRequirement requirement = RelationshipRequirement.Source) {
			switch (requirement) {
				case RelationshipRequirement.Source:
					return relationships.FirstOrDefault(x => x.Involves(relationship) && x.InvolvesSource(this));
				case RelationshipRequirement.Destination:
					return relationships.FirstOrDefault(x => x.Involves(relationship) && x.InvolvesDestination(this));
				case RelationshipRequirement.Either:
				default:
					return relationships.FirstOrDefault(x => x.Involves(relationship) && x.Involves(this));
			}
		}

		public List<RelationshipInstance> GetRelationshipsWith(NounInstance other, RelationshipRequirement otherIs = RelationshipRequirement.Source) {
			switch (otherIs) {
				case RelationshipRequirement.Source:
					return relationships.Where(relationship => relationship.InvolvesSource(other)).ToList();
				case RelationshipRequirement.Destination:
					return relationships.Where(relationship => relationship.InvolvesDestination(other)).ToList();
				case RelationshipRequirement.Either:
				default:
					return relationships.Where(relationship => relationship.Involves(other)).ToList();
			}
		}

		public List<RelationshipInstance> GetRelationshipsWith(RelationshipScriptableObject relationship) {
			return relationships.Where(x => x.Involves(relationship)).ToList();
		}

		public bool HasRelationship(RelationshipScriptableObject relationship, NounInstance other, RelationshipRequirement requirement = RelationshipRequirement.Source) {
			return GetRelationship(relationship, other, requirement) != null;
		}

		public bool TryGetRelationship(RelationshipScriptableObject relationship, NounInstance other, out RelationshipInstance result, RelationshipRequirement requirement = RelationshipRequirement.Source) {
			result = GetRelationship(relationship, other, requirement);
			return result != null;
		}

		public void AddRelationship(RelationshipInstance relationship) {
			if (!relationships.Contains(relationship)) {
				relationships.Add(relationship);
			}
		}

		public void AddRelationship(RelationshipScriptableObject relationship, NounInstance other, SourceOrDestination sourceOrDestination = SourceOrDestination.Source) {
			// if the character already has the relationship -> don't do anything
			if (HasRelationship(relationship, other, sourceOrDestination.AsRequirement())) {
				return;
			}
			var relationshipInstance = new RelationshipInstance(relationship, this, other, sourceOrDestination);
			this.AddRelationship(relationshipInstance);
			other.AddRelationship(relationshipInstance);
		}

		public void RemoveRelationship(RelationshipInstance relationship, bool removeFromOther = true) {
			relationships.Remove(relationship);
			if (removeFromOther) {
				var otherUid = relationship.SourceUID != this.uid ? relationship.SourceUID : relationship.DestinationUID;
				var otherInstance = NarramancerSingleton.Instance.GetInstance(otherUid);
				otherInstance.RemoveRelationship(relationship, false);
			}
		}

		public void RemoveRelationship(RelationshipScriptableObject relationship, RelationshipRequirement requirement = RelationshipRequirement.Source) {
			var instance = GetRelationship(relationship, requirement);
			if (instance != null) {
				this.RemoveRelationship(instance, true);
			}
		}

		public void RemoveRelationship(RelationshipScriptableObject relationship, NounInstance other, RelationshipRequirement requirement = RelationshipRequirement.Source) {
			if (TryGetRelationship(relationship, other, out var instance, requirement)) {
				this.RemoveRelationship(instance, false);
				other.RemoveRelationship(instance, false);
			}
		}

		public void AddRelationships(IEnumerable<RelationshipAssignment> assignments, IEnumerable<NounInstance> instances) {
			foreach (var assignment in assignments) {
				var otherInstance = instances.FirstOrDefault(instance => instance.Noun == assignment.other);
				if (otherInstance != null) {
					AddRelationship(assignment.relationship, otherInstance, assignment.sourceOrDestination);
				}
			}
		}

		#endregion
	}
}