namespace WebMatrix.Data.StronglyTyped.Tests {
	using System;
	using System.ComponentModel.DataAnnotations;
	using System.Data.SqlServerCe;
	using System.IO;
	using NUnit.Framework;
	using System.Linq;
	using Should;

	[TestFixture]
	public class ExecuteTests {

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

				cmd.CommandText = "create table Users (Id int identity not null, Name nvarchar(250))";
				cmd.ExecuteNonQuery();
			}

			StrongTypingExtensions.Log = Console.Out;

		}

		[SetUp]
		public void Setup() {
			using (var db = Database.Open("Test")) {
				db.Execute("delete from Users");
			}
		}

		[Test]
		public void Inserts_record() {
			using(var db = Database.Open("Test")) {
				db.Insert(new User { Name = "Foo" });

				var result = db.Query<User>("select * from users").Single();
				result.Id.ShouldEqual(1);
				result.Name.ShouldEqual("Foo");
			}
		}
		[Test]
		public void Inserts_record_with_aliased_column() {
			using(var db = Database.Open("Test")) {
				db.Insert(new UserWithAliasedProperty { OtherName = "FooAliased" });

				var result = db.Query<User>("select * from users").Single();
				result.Name.ShouldEqual("FooAliased");
			}
		}

		[Test]
		public void Updates_record() {
			using (var db = Database.Open("Test")) {
				var user = new User { Name = "Foo" };
				db.Insert(user);
				user.Name = "Bar";
				db.Update(user);

				var result = db.Query<User>("select * from users").Single();
				result.Name.ShouldEqual("Bar");
			}
		}


		[Test]
		public void Inserts_with_store_generated_id() {
			using(var db = Database.Open("Test")) {
				var user = new User{ Name = "Foo" };
				db.Insert(user);
				
				user.Id.ShouldNotEqual(0);
			}
		}

		[Test]
		public void Inserts_with_store_generated_id_when_id_is_implicit() {
			using (var db = Database.Open("Test")) {
				var user = new UserWithImplicitId { Name = "Foo" };
				db.Insert(user);

				user.Id.ShouldNotEqual(0);
			}
		}


		[Table("Users")]
		public class User {
			[Key]
			public int Id { get; set; }
			public string Name { get; set; }
		}

		[Table("Users")]
		public class UserWithImplicitId {
			public int Id { get; set; }
			public string Name { get; set; }
		}

		[Table("Users")]
		public class UserWithAliasedProperty {
			public int Id { get; set; }
			[Column("Name")]
			public string OtherName { get; set; }
		}

	}

}