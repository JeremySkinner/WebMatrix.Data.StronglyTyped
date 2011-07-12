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
	using System;
	using System.ComponentModel.DataAnnotations;
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

		[SetUp]
		public void Setup() {
			using(var db = Database.Open("Test")) {
				db.Execute("delete from Users");
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

		[Test]
		public void Gets_property_with_remapped_name() {
			using(var db = Database.Open("Test")) {
				db.Execute("insert into Users (Id, Name) values (1, 'Jeremy')");

				var results = db.Query<User6>("select * from Users").ToList();
				results.Count.ShouldEqual(1);
				results[0].OtherName.ShouldEqual("Jeremy");
			}
		}

		[Test]
		public void Gets_property_case_insensitively() {
			using(var db = Database.Open("Test")) {
				db.Execute("insert into Users (Id, Name) values (1, 'Jeremy')");

				var results = db.Query<User7>("select * from Users").ToList();
				results.Count.ShouldEqual(1);
				results[0].name.ShouldEqual("Jeremy");
			}
		}

		[Test]
		public void Does_not_throw_when_object_does_not_have_property() {
			using(var db = Database.Open("Test")) {
				db.Execute("insert into Users (Id, Name) values (1, 'Jeremy')");

				var results = db.Query<User>("select Name as Foo from Users").ToList();
				results.Single().Name.ShouldBeNull();
			}
		}

		[Test]
		public void Does_not_throw_when_property_not_settable() {
			using(var db = Database.Open("Test")) {
				db.Execute("insert into Users (Id, Name) values (1, 'Jeremy')");

				db.Query<User2>("select Name from Users").ToList();
			}
		}

		[Test]
		public void Throws_when_property_of_wrong_type() {
			using (var db = Database.Open("Test")) {
				db.Execute("insert into Users (Id, Name) values (1, 'Jeremy')");

				var ex = Assert.Throws<MappingException>(() => db.Query<User3>("select Name from Users").ToList());
				ex.Message.ShouldEqual("Could not map the property 'Name' as its data type does not match the database.");
			}
		}

		[Test]
		public void Throws_when_cannot_instantiate_object() {
			using(var db = Database.Open("Test")) {
				db.Execute("insert into Users (Id, Name) values (1, 'Jeremy')");

				var ex = Assert.Throws<MappingException>(() => db.Query<User4>("select * from Users").ToList());
				ex.Message.ShouldEqual("Could not find a parameterless constructor on the type 'WebMatrix.Data.StronglyTyped.Tests.QueryTests+User4'. WebMatrix.Data.StronglyTyped can only be used to map types that have a public, parameterless constructor.");

			}
		}

		[Test]
		public void Does_not_map_properties_with_NotMapped_attribute() {
			using (var db = Database.Open("Test")) {
				db.Execute("insert into Users (Id, Name) values (1, 'Jeremy')");

				var result = db.Query<User5>("select * from Users").Single();
				result.Id.ShouldEqual(1);
				result.Name.ShouldBeNull();
			}
		}

		[Test]
		public void FindAll_gets_all() {
			using(var db = Database.Open("Test")) {
				db.Execute("insert into Users (Id, Name) values (1, 'Jeremy')");
				db.Execute("insert into Users (Id, Name) values (2, 'Jeremy')");

				var result = db.FindAll<User>();
				result.Count().ShouldEqual(2);
			}
		}

		[Test]
		public void FindById_finds_by_id() {
			using (var db = Database.Open("Test")) {
				db.Execute("insert into Users (Id, Name) values (1, 'Jeremy')");

				var result = db.FindById<User>(1);
				result.Name.ShouldEqual("Jeremy");
			}
		}

		[Table("Users")]
		public class User {
			[Key]
			public int Id { get; set; }
			public string Name { get; set; }
		}

		[Table("Users")]
		public class User2 {
			public string Name {
				get { return null; }
			}
		}

		[Table("Users")]
		public class User3 {
			public int Name { get; set; }
		}

		[Table("Users")]
		public class User4 : User {
			public User4(int x) {
				
			}
		}

		[Table("Users")]
		public class User5 {
			public int Id { get; set; }
			[NotMapped]
			public string Name { get; set; }
		}

		[Table("Users")]
		public class User6 {
			public int Id { get; set; }
			[Column("Name")]
			public string OtherName { get; set; }
		}

		[Table("Users")]
		public class User7 {
			public int Id { get; set; }
			public string name { get; set; }
		}
	}
}