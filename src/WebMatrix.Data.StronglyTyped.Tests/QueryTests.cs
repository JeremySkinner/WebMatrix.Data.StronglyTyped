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

namespace WebMatrix.Data.StronglyTyped.Tests {
	using System.Data.SqlServerCe;
	using System.IO;
	using System.Linq;
	using NUnit.Framework;
	using WebMatrix.Data.StronglyTyped;
	using Should;

	[TestFixture]
	public class QueryTests {
		private string connString = "Data Source='Test.sdf'";

		[TestFixtureSetUp]
		public void TestFixtureSetup() {
			// Initialize the database.

			if (File.Exists("Test.sdf")) {
				File.Delete("Test.sdf");
			}

			using (var engine = new SqlCeEngine(connString)) {
				engine.CreateDatabase();
			}

			using (var conn = new SqlCeConnection(connString)) {
				var cmd = conn.CreateCommand();
				conn.Open();

				cmd.CommandText = "create table Users (Id int, Name nvarchar(250))";
				cmd.ExecuteNonQuery();
			}
		}

		[Test]
		public void Gets_data_strongly_typed() {
			using (var db = Database.Open("Test")) {
				db.Execute("insert into Users (Id, Name) values (1, 'Jeremy')");
				db.Execute("insert into Users (Id, Name) values (2, 'Bob')");

				var results = db.Query<User>("select * from Users").ToList();
				results.Count.ShouldEqual(2);

				results[0].Id.ShouldEqual(1);
				results[0].Name.ShouldEqual("Jeremy");

				results[1].Id.ShouldEqual(2);
				results[1].Name.ShouldEqual("Bob");
			}
		}


		public class User {
			public int Id { get; set; }
			public string Name { get; set; }
		}
	}
}