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
	using System.Text;

	public class Mapper<T> {
		readonly Func<T> factory;
		readonly Dictionary<string, PropertyMetadata<T>> properties;
		static readonly Lazy<Mapper<T>> instanceCache = new Lazy<Mapper<T>>(() => new Mapper<T>());

		public string TableName { get; private set; }

		public static Mapper<T> Create() {
			return instanceCache.Value;
		}

		private Mapper() {
			this.properties = new Dictionary<string, PropertyMetadata<T>>(StringComparer.InvariantCultureIgnoreCase);

			factory = CreateActivatorDelegate();

			var attribute = (TableAttribute)Attribute.GetCustomAttribute(typeof(T), typeof(TableAttribute));
			TableName = attribute != null ? attribute.Name : typeof(T).Name;

			// Get all properties that are writeable without a NotMapped attribute.
			var properties = from property in typeof(T).GetProperties()
						where property.CanWrite
						let notMappedAttr = (NotMappedAttribute)Attribute.GetCustomAttribute(property, typeof(NotMappedAttribute))
						where notMappedAttr == null
						select property;

			foreach(var property in properties) {
				this.properties[property.Name] = new PropertyMetadata<T>(property);
			}
		}

		public T Map(DynamicRecord record) {
			var instance = factory();

			foreach (var column in record.Columns) {
				PropertyMetadata<T> property;

				if (properties.TryGetValue(column, out property)) {
					try {
						property.SetValue(instance, record[column]);
					}
					catch(InvalidCastException e) {
						throw MappingException.InvalidCast(column, e);
					}
				}
			}

			return instance;
		}

		public Tuple<string, object[]> MapToInsert(T toInsert) {
			string insert = "insert into [{0}] ({1}) values ({2})";
			var columns = new List<string>();
			var parameterNames = new List<string>();
			var parameters = new List<object>();

			int parameterCounter = 0;

			foreach(var property in properties.Values) {
				if(property.IsId) continue; // assume ID properties are store-generated.
				
				columns.Add(property.Property.Name);
				parameterNames.Add("@" + parameterCounter++);
				parameters.Add(property.GetValue(toInsert));
			}
		
			insert = string.Format(insert, 
				TableName, 
				string.Join(", ", columns), 
				string.Join(", ", parameterNames));

			return Tuple.Create(insert, parameters.ToArray());
		}

		public Tuple<string, object[]> MapToUpdate(T toUpdate) {
			string update = "update [{0}] set {1} where {2}";
			
			var idColumns = new List<string>();

			var columns = new List<string>();
			var parameters = new List<object>();
			int parameterCount = 0;


			foreach(var property in properties.Values) {
				if(property.IsId) {
					idColumns.Add(property.Property.Name + " = @" + parameterCount++);
					parameters.Add(property.GetValue(toUpdate));
				}
				else {
					columns.Add(property.Property.Name + " = @" + parameterCount++);
					parameters.Add(property.GetValue(toUpdate)??DBNull.Value);
				}
			}

			if(idColumns.Count == 0) {
				throw new NotSupportedException(string.Format("Could not determine Primary Key properties for {0}", typeof(T).Name));
			}

			update = string.Format(
				update, 
				TableName, 
				string.Join(", ", columns),
				string.Join(" AND ",  idColumns)
			);

			return Tuple.Create(update, parameters.ToArray());	

		}


		private static Func<T> CreateActivatorDelegate() {
			var constructor = typeof (T).GetConstructor(Type.EmptyTypes);

			// No parameterless constructor found.
			if(constructor == null) {
				return () => { throw MappingException.NoParameterlessConstructor(typeof(T)); };
			}

			return Expression.Lambda<Func<T>>(Expression.New(constructor)).Compile();
		}

		public PropertyMetadata<T> GetIdColumn() {
			var cols = this.properties.Where(x => x.Value.IsId).Select(x => x.Value).ToList();
			if(cols.Count > 1) {
				throw new NotSupportedException(string.Format("Multiple PK properties were defined on type {0}", typeof(T).Name));
			}
			if(cols.Count == 0) {
				throw new NotSupportedException(string.Format("No PK properties were defined on type {0}.", typeof (T).Name));
			}

			return cols.Single();
		}
	}
}