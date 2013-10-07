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
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	public static class StrongTypingExtensions {
		public static TextWriter Log = TextWriter.Null;

		public static int Delete<T>(this Database db, string where = null, params object[] args) {
			var mapper = Mapper<T>.Create();
			var query = mapper.MapToDelete() + (where ?? "");

			Log.WriteLine(query);

			return db.Execute(query, args);
		}

		public static IEnumerable<T> QuerySql<T>(this Database db, string commandText, params object[] args) {
			Log.WriteLine(commandText);

			var queryResults = db.Query(commandText, args);
			var mapper = Mapper<T>.Create();
			return queryResults.Select<dynamic, T>(record => mapper.Map(record)).ToList();
		}

		public static IEnumerable<T> Query<T>(this Database db, string where = null, params object[] args) {
			var mapper = Mapper<T>.Create();
			var query = mapper.MapToSelect() + (where ?? "");

			Log.WriteLine(query);
			
			return db.QuerySql<T>(query, args);
		}

		public static IEnumerable<T> FindAll<T>(this Database db) {
			var mapper = Mapper<T>.Create();
			var query = mapper.MapToSelect();
	
			Log.WriteLine(query);

			return db.QuerySql<T>(query);
		}

		public static T FindById<T>(this Database db, object id) {
			var mapper = Mapper<T>.Create();
			var idCol = mapper.GetIdColumn();
			var query = mapper.MapToSelect() + string.Format("where {0} = @0", idCol.PropertyName);

			Log.WriteLine(query);

			return db.QuerySql<T>(query, id).SingleOrDefault();
		}

		public static void Insert<T>(this Database db, T toInsert) {
			var mapper = Mapper<T>.Create();
			var results = mapper.MapToInsert(toInsert);
			var sql = results.Item1;
			var parameters = results.Item2;

			Log.WriteLine(sql);

			db.Execute(sql, parameters);

			//TODO: Don't assume ID will always be an int.

			var id = (int)db.GetLastInsertId();
			var idColumn = mapper.GetIdColumn();
			idColumn.SetValue(toInsert, id);
		}

		public static void Update<T>(this Database db, T toUpdate) {
			var mapper = Mapper<T>.Create();
			var results = mapper.MapToUpdate(toUpdate);
			var sql = results.Item1;
			var parameters = results.Item2;

			Log.WriteLine(sql);

			db.Execute(sql, parameters);
		}
	}
}