namespace WebMatrix.Data.StronglyTyped {
	using System;

	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class ColumnAttribute : Attribute {
		public ColumnAttribute(string name) {
			Name = name;
		}
		public string Name { get; private set; }
	}
}