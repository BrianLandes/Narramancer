using System;

namespace Narramancer {
	[AttributeUsage(AttributeTargets.Field)]
	public class VerbRequiredAttribute : Attribute {

	}

	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public class RequireInputAttribute : Attribute {

		public Type RequiredType { get; set; }
		public string DefaultName { get; set; }

		public RequireInputAttribute() { }
		public RequireInputAttribute(Type requiredType, string defaultName = null) {
			RequiredType = requiredType;
			DefaultName = defaultName;
		}
	}

	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public class RequireOutputAttribute : Attribute {

		public Type RequiredType { get; set; }
		public string DefaultName { get; set; }

		public RequireOutputAttribute() { }
		public RequireOutputAttribute(Type requiredType, string defaultName = null) {
			RequiredType = requiredType;
			DefaultName = defaultName;
		}
	}

	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public class RequireInputFromSerializableTypeAttribute : Attribute {

		public string TypeFieldName { get; set; }
		public string DefaultName { get; set; }

		public RequireInputFromSerializableTypeAttribute() { }
		public RequireInputFromSerializableTypeAttribute(string typeFieldName, string defaultName = null) {
			TypeFieldName = typeFieldName;
			DefaultName = defaultName;
		}
	}

	[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
	public class RequireOutputFromSerializableTypeAttribute : Attribute {

		public string TypeFieldName { get; set; }
		public string DefaultName { get; set; }

		public RequireOutputFromSerializableTypeAttribute() { }
		public RequireOutputFromSerializableTypeAttribute(string typeFieldName, string defaultName = null) {
			TypeFieldName = typeFieldName;
			DefaultName = defaultName;
		}
	}
}