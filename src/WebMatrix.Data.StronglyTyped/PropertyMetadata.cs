namespace WebMatrix.Data.StronglyTyped {
	using System;
	using System.ComponentModel.DataAnnotations;
	using System.Linq.Expressions;
	using System.Reflection;

	public class PropertyMetadata<T> {
		private readonly Action<T, object> setter;
		private readonly PropertyInfo property;
		private readonly Func<T, object> getter;

		public PropertyMetadata(PropertyInfo property) {
			this.property = property;
			this.setter = BuildSetterDelegate(property);
			this.getter = BuildGetterDelegate(property);
			IsId = Attribute.IsDefined(property, typeof(KeyAttribute)) || property.Name == "Id" || property.Name == "ID";
		}

		public bool IsId { get; private set; }

		public PropertyInfo Property {
			get { return property; }
		}

		public void SetValue(T instance, object value) {
			setter(instance, value);
		}

		public object GetValue(T instance) {
			return getter(instance);
		}

		private static Action<T, object> BuildSetterDelegate(PropertyInfo prop) {
			var instance = Expression.Parameter(typeof(T), "x");
			var argument = Expression.Parameter(typeof(object), "v");

			var setterCall = Expression.Call(
				instance,
				prop.GetSetMethod(true),
				Expression.Convert(argument, prop.PropertyType));


			return (Action<T, object>)Expression.Lambda(setterCall, instance, argument).Compile();
		}

		private Func<T, object> BuildGetterDelegate(PropertyInfo prop) {
			var param = Expression.Parameter(typeof(T), "x");
			Expression expression = Expression.PropertyOrField(param, prop.Name);

			if (prop.PropertyType.IsValueType)
				expression = Expression.Convert(expression, typeof(object));

			return Expression.Lambda<Func<T, object>>(expression, param)
				.Compile();
		}


	}
}