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
	using System.Linq;
	using System.Linq.Expressions;
	using System.Reflection;

	public class Mapper<T> {
		readonly Func<T> factory;
		readonly Dictionary<string, Action<T, object>> setters = new Dictionary<string, Action<T, object>>();
		static readonly Lazy<Mapper<T>> instanceCache = new Lazy<Mapper<T>>(() => new Mapper<T>());

		public static Mapper<T> Create() {
			return instanceCache.Value;
		}

		private Mapper() {
			factory = CreateActivatorDelegate();

			// Get all properties that are writeable without a NotMapped attribute.
			var properties = from property in typeof(T).GetProperties()
						where property.CanWrite
						let notMappedAttr = (NotMappedAttribute)Attribute.GetCustomAttribute(property, typeof(NotMappedAttribute))
						where notMappedAttr == null
						select new { property.Name, Setter = BuildSetterDelegate(property) };

			foreach(var property in properties) {
				setters[property.Name] = property.Setter;
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
			var constructor = typeof (T).GetConstructor(Type.EmptyTypes);

			// No parameterless constructor found.
			if(constructor == null) {
				return () => { throw MappingException.NoParameterlessConstructor(typeof(T)); };
			}

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