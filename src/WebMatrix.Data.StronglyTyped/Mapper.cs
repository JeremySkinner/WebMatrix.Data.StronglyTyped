#region Licence

// Copyright (c) Jeremy Skinner (http://www.jeremyskinner.co.uk)
// 
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at 
// 
// http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, 
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and 
// limitations under the License.

#endregion

namespace WebMatrix.Data.StronglyTyped {
	using System;
	using System.Collections.Generic;
	using System.Linq.Expressions;
	using System.Reflection;

	public class Mapper<T> {
		private Func<T> factory;
		private Dictionary<string, Action<T, object>> setters = new Dictionary<string, Action<T, object>>();
		private static Lazy<Mapper<T>> _instance = new Lazy<Mapper<T>>(() => new Mapper<T>());

		public static Mapper<T> Create() {
			return _instance.Value;
		}

		private Mapper() {
			factory = CreateActivatorDelegate();

			foreach (var property in typeof (T).GetProperties()) {
				if (property.CanWrite) {
					setters[property.Name] = BuildSetterDelegate(property);
				}
			}
		}

		public T Map(DynamicRecord record) {
			var instance = factory();

			foreach (var column in record.Columns) {
				Action<T, object> setter;

				if (setters.TryGetValue(column, out setter)) {
					try {
						setter(instance, record[column]);
					}
					catch(InvalidCastException e) {
						throw MappingException.InvalidCast(column, e);
					}
				}
			}

			return instance;
		}


		private static Func<T> CreateActivatorDelegate() {
			var constructorInfo = typeof (T).GetConstructor(Type.EmptyTypes);

			// No parameterless constructor found.
			if(constructorInfo == null) {
				return () => { throw MappingException.NoParameterlessConstructor(typeof(T)); };
			}

			return CreateActivatorDelegate(constructorInfo);
		}

		private static Func<T> CreateActivatorDelegate(ConstructorInfo constructor) {
			return Expression.Lambda<Func<T>>(Expression.New(constructor)).Compile();
		}

		private static Action<T, object> BuildSetterDelegate(PropertyInfo prop) {
			var instance = Expression.Parameter(typeof (T), "x");
			var argument = Expression.Parameter(typeof (object), "v");

			var setterCall = Expression.Call(
				instance,
				prop.GetSetMethod(true),
				Expression.Convert(argument, prop.PropertyType));


			return (Action<T, object>) Expression.Lambda(setterCall, instance, argument).Compile();
		}
	}
}