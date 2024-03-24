
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
            return id.Value;
        }
        public static implicit operator NounUID(string id) {
            return new NounUID(id);
        }

        public override bool Equals(object obj) {
            if (obj is NounUID otherId) {
                return Value.Equals(otherId.Value, StringComparison.Ordinal);
            }
            return base.Equals(obj);
        }

        public override string ToString() {
            return Value;
        }

        public override int GetHashCode() {
            return Value.GetHashCode();
        }
    }
}