
using System;
using UnityEngine;
namespace Narramancer {
    [Serializable]
    public class NounUID {

        [SerializeField]
        private string Value = "";

        public NounUID() {
            GenerateNew();
        }

        public NounUID(string value) {
            Value = value;
        }

        public void GenerateNew() {
            Value = Guid.NewGuid().ToString();
        }

        public static implicit operator string(NounUID id) {
            return id?.Value;
        }
        public static implicit operator NounUID(string id) {
            return new NounUID(id);
        }

        /// <summary>
        /// Value equality. Without an explicit operator, <c>==</c> on two <see cref="NounUID"/>s falls back to
        /// *reference* equality: the implicit string conversion is a user-defined conversion, and predefined
        /// operator resolution doesn't consider those, so <c>string ==</c> never becomes a candidate.
        /// <para>
        /// That mattered. <see cref="RelationshipInstance"/> matches participants with
        /// <c>source.UID == this.source</c>, and it only worked because <c>RelationshipInstance.source</c>
        /// holds the very same object as <c>NounInstance.uid</c> — so relationship integrity quietly depended
        /// on the serializer preserving shared references across a save. Comparing by value removes that
        /// dependency, and fixes <c>NarramancerSingleton.GetInstance(uid)</c>, which returned null for every
        /// caller that passed a UID string (the implicit conversion built a new, reference-unequal object).
        /// </para>
        /// </summary>
        public static bool operator ==(NounUID left, NounUID right) {
            if (ReferenceEquals(left, right)) {
                return true;
            }
            if (left is null || right is null) {
                return false;
            }
            return string.Equals(left.Value, right.Value, StringComparison.Ordinal);
        }

        public static bool operator !=(NounUID left, NounUID right) {
            return !(left == right);
        }

        public override bool Equals(object obj) {
            switch (obj) {
                case NounUID otherId:
                    return string.Equals(Value, otherId.Value, StringComparison.Ordinal);
                case string otherValue:
                    // A UID and its string form are interchangeable at call sites thanks to the implicit
                    // conversion, so Equals agrees with == rather than contradicting it.
                    return string.Equals(Value, otherValue, StringComparison.Ordinal);
                default:
                    return false;
            }
        }

        public override string ToString() {
            return Value;
        }

        public override int GetHashCode() {
            return Value?.GetHashCode() ?? 0;
        }
    }
}