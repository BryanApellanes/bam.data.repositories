using Bam.Test;
using Bam.Test.Integration;
using MongoDB.Bson;
using MongoDB.Driver;

namespace Bam.Data.Repositories.Tests.Integration
{
    /// <summary>
    /// Exercises <see cref="MongoRepository"/> against a real MongoDB server (bam.data.repositories#6). Start one
    /// with <c>start-test-containers.sh</c> at the repository root (podman, container <c>bam-data-test-mongo</c>,
    /// port 27017). Each test works in its own database and drops it afterwards.
    /// </summary>
    [IntegrationTestMenu("MongoRepository should", "mongo")]
    public class MongoRepositoryShould : IntegrationTestMenuContainer
    {
        private const string ConnectionString = "mongodb://localhost:27017";

        [IntegrationTest]
        public void CreateAndRetrieveById()
        {
            When.A<MongoTestScope>("creates a widget and retrieves it by each id overload",
                MongoTestScope.Open,
                scope =>
                {
                    using (scope)
                    {
                        MongoWidget created = scope.Repository.Create(new MongoWidget { Id = 7, Uuid = "uuid-7", Name = "Widget", Quantity = 3 });
                        MongoWidget byUnsignedLong = scope.Repository.Retrieve<MongoWidget>(7UL);
                        MongoWidget byLong = scope.Repository.Retrieve<MongoWidget>(7L);
                        MongoWidget byInt = scope.Repository.Retrieve<MongoWidget>(7);
                        MongoWidget missing = scope.Repository.Retrieve<MongoWidget>(8UL);
                        return new RetrieveOutcome(created != null, byUnsignedLong?.Name, byLong?.Quantity, byInt?.Uuid, missing == null);
                    }
                })
                .TheTest
                .ShouldPass<RetrieveOutcome>((because, outcome) =>
                {
                    because.ItsTrue("Create returns the instance", outcome.Created);
                    because.ItsTrue("retrieve by ulong finds it", outcome.NameByUnsignedLong == "Widget");
                    because.ItsTrue("retrieve by long finds it", outcome.QuantityByLong == 3);
                    because.ItsTrue("retrieve by int finds it", outcome.UuidByInt == "uuid-7");
                    because.ItsTrue("an unknown id retrieves null", outcome.MissingIsNull);
                })
                .SoBeHappy()
                .UnlessItFailed();
        }

        [IntegrationTest]
        public void RetrieveByUuid()
        {
            When.A<MongoTestScope>("retrieves a widget by its uuid",
                MongoTestScope.Open,
                scope =>
                {
                    using (scope)
                    {
                        scope.Repository.Create(new MongoWidget { Id = 1, Uuid = "first", Name = "One" });
                        scope.Repository.Create(new MongoWidget { Id = 2, Uuid = "second", Name = "Two" });
                        MongoWidget found = scope.Repository.Retrieve<MongoWidget>("second");
                        MongoWidget missing = scope.Repository.Retrieve<MongoWidget>("third");
                        return new UuidOutcome(found?.Id, found?.Name, missing == null);
                    }
                })
                .TheTest
                .ShouldPass<UuidOutcome>((because, outcome) =>
                {
                    because.ItsTrue("the matching widget is returned", outcome.Id == 2 && outcome.Name == "Two");
                    because.ItsTrue("an unknown uuid retrieves null", outcome.MissingIsNull);
                })
                .SoBeHappy()
                .UnlessItFailed();
        }

        [IntegrationTest]
        public void UpdateAnExistingDocumentAndInsertAMissingOne()
        {
            When.A<MongoTestScope>("updates a stored widget and one that was never created",
                MongoTestScope.Open,
                scope =>
                {
                    using (scope)
                    {
                        scope.Repository.Create(new MongoWidget { Id = 1, Uuid = "one", Name = "Original", Quantity = 1 });
                        scope.Repository.Update(new MongoWidget { Id = 1, Uuid = "one", Name = "Changed", Quantity = 2 });
                        scope.Repository.Update(new MongoWidget { Id = 2, Uuid = "two", Name = "Upserted" });
                        MongoWidget changed = scope.Repository.Retrieve<MongoWidget>(1UL);
                        MongoWidget upserted = scope.Repository.Retrieve<MongoWidget>(2UL);
                        return new UpdateOutcome(changed?.Name, changed?.Quantity, upserted?.Name, scope.Count<MongoWidget>());
                    }
                })
                .TheTest
                .ShouldPass<UpdateOutcome>((because, outcome) =>
                {
                    because.ItsTrue("the stored widget carries the new values", outcome.ChangedName == "Changed" && outcome.ChangedQuantity == 2);
                    because.ItsTrue("the missing widget was inserted", outcome.UpsertedName == "Upserted");
                    because.ItsTrue("the update replaced rather than duplicated", outcome.DocumentCount == 2);
                })
                .SoBeHappy()
                .UnlessItFailed();
        }

        [IntegrationTest]
        public void DeleteByKey()
        {
            When.A<MongoTestScope>("deletes one of two widgets",
                MongoTestScope.Open,
                scope =>
                {
                    using (scope)
                    {
                        MongoWidget doomed = scope.Repository.Create(new MongoWidget { Id = 1, Uuid = "one", Name = "Doomed" });
                        scope.Repository.Create(new MongoWidget { Id = 2, Uuid = "two", Name = "Kept" });
                        bool deleted = scope.Repository.Delete(doomed);
                        bool deletedAgain = scope.Repository.Delete(doomed);
                        return new DeleteOutcome(deleted, deletedAgain, scope.Repository.Retrieve<MongoWidget>(1UL) == null, scope.Repository.Retrieve<MongoWidget>(2UL)?.Name, scope.Count<MongoWidget>());
                    }
                })
                .TheTest
                .ShouldPass<DeleteOutcome>((because, outcome) =>
                {
                    because.ItsTrue("Delete reports success", outcome.Deleted);
                    because.ItsTrue("deleting what is already gone still reports success", outcome.DeletedAgain);
                    because.ItsTrue("the deleted widget is gone", outcome.DoomedIsGone);
                    because.ItsTrue("the other widget is untouched", outcome.KeptName == "Kept" && outcome.DocumentCount == 1);
                })
                .SoBeHappy()
                .UnlessItFailed();
        }

        [IntegrationTest]
        public void QueryWithEachFilterForm()
        {
            When.A<MongoTestScope>("queries widgets with a filter definition, a document, JSON, an object and null",
                MongoTestScope.Open,
                scope =>
                {
                    using (scope)
                    {
                        scope.Repository.Create(new MongoWidget { Id = 1, Uuid = "a", Name = "Apple", Quantity = 5 });
                        scope.Repository.Create(new MongoWidget { Id = 2, Uuid = "b", Name = "Banana", Quantity = 5 });
                        scope.Repository.Create(new MongoWidget { Id = 3, Uuid = "c", Name = "Cherry", Quantity = 9 });

                        FilterDefinition<BsonDocument> definition = Builders<BsonDocument>.Filter.Gt(nameof(MongoWidget.Quantity), 5);
                        dynamic document = new BsonDocument(nameof(MongoWidget.Quantity), 5);
                        dynamic json = "{ Name: \"Banana\" }";
                        dynamic filterObject = new NameFilter { Name = "Apple" };
                        dynamic? nothing = null;

                        IEnumerable<MongoWidget> byDefinition = scope.Repository.Query<MongoWidget>(definition);
                        IEnumerable<MongoWidget> byDocument = scope.Repository.Query<MongoWidget>(document);
                        IEnumerable<MongoWidget> byJson = scope.Repository.Query<MongoWidget>(json);
                        IEnumerable<MongoWidget> byObject = scope.Repository.Query<MongoWidget>(filterObject);
                        IEnumerable<MongoWidget> all = scope.Repository.Query<MongoWidget>(nothing);
                        return new QueryOutcome(
                            byDefinition.Select(w => w.Name).ToArray(),
                            byDocument.Select(w => w.Name).OrderBy(n => n).ToArray(),
                            byJson.Select(w => w.Name).ToArray(),
                            byObject.Select(w => w.Name).ToArray(),
                            all.Count());
                    }
                })
                .TheTest
                .ShouldPass<QueryOutcome>((because, outcome) =>
                {
                    because.ItsTrue("a filter definition is applied", outcome.ByDefinition.SequenceEqual(new[] { "Cherry" }));
                    because.ItsTrue("a BsonDocument is applied", outcome.ByDocument.SequenceEqual(new[] { "Apple", "Banana" }));
                    because.ItsTrue("a JSON string is applied", outcome.ByJson.SequenceEqual(new[] { "Banana" }));
                    because.ItsTrue("an object's properties are matched for equality", outcome.ByObject.SequenceEqual(new[] { "Apple" }));
                    because.ItsTrue("a null query matches everything", outcome.AllCount == 3);
                })
                .SoBeHappy()
                .UnlessItFailed();
        }

        [IntegrationTest]
        public void RecordTheFailureWhenAKeyIsReused()
        {
            When.A<MongoTestScope>("creates two widgets with the same id",
                MongoTestScope.Open,
                scope =>
                {
                    using (scope)
                    {
                        bool createFailedRaised = false;
                        scope.Repository.CreateFailed += (_, _) => createFailedRaised = true;
                        scope.Repository.Create(new MongoWidget { Id = 1, Uuid = "one", Name = "First" });
                        MongoWidget second = scope.Repository.Create(new MongoWidget { Id = 1, Uuid = "other", Name = "Second" });
                        return new FailureOutcome(second == null, scope.Repository.LastException is MongoWriteException, createFailedRaised, scope.Repository.Retrieve<MongoWidget>(1UL)?.Name);
                    }
                })
                .TheTest
                .ShouldPass<FailureOutcome>((because, outcome) =>
                {
                    because.ItsTrue("the second Create returns null", outcome.SecondIsNull);
                    because.ItsTrue("LastException holds the write error", outcome.WriteExceptionRecorded);
                    because.ItsTrue("CreateFailed is raised", outcome.CreateFailedRaised);
                    because.ItsTrue("the first widget is still stored", outcome.StoredName == "First");
                })
                .SoBeHappy()
                .UnlessItFailed();
        }

        [IntegrationTest]
        public void RoundTripATypeWhoseKeyIsNotTheDocumentId()
        {
            When.A<MongoTestScope>("creates, retrieves, updates and deletes a type keyed by a [Key] property",
                MongoTestScope.Open,
                scope =>
                {
                    using (scope)
                    {
                        scope.Repository.Create(new MongoKeyedThing { Number = 41, Name = "Original" });
                        string? retrieved = scope.Repository.Retrieve<MongoKeyedThing>(41UL)?.Name;
                        scope.Repository.Update(new MongoKeyedThing { Number = 41, Name = "Changed" });
                        string? updated = scope.Repository.Retrieve<MongoKeyedThing>(41UL)?.Name;
                        long countAfterUpdate = scope.Count<MongoKeyedThing>();
                        scope.Repository.Delete(new MongoKeyedThing { Number = 41 });
                        return new KeyedOutcome(retrieved, updated, countAfterUpdate, scope.Count<MongoKeyedThing>());
                    }
                })
                .TheTest
                .ShouldPass<KeyedOutcome>((because, outcome) =>
                {
                    because.ItsTrue("it is retrieved by its key", outcome.Retrieved == "Original");
                    because.ItsTrue("it is updated in place", outcome.Updated == "Changed" && outcome.CountAfterUpdate == 1);
                    because.ItsTrue("it is deleted by its key", outcome.CountAfterDelete == 0);
                })
                .SoBeHappy()
                .UnlessItFailed();
        }

        [IntegrationTest]
        public void QueryWithEachFilterFormThroughTheBaseRepository()
        {
            When.A<MongoTestScope>("queries widgets through a Repository reference, where only the dynamic overload is visible",
                MongoTestScope.Open,
                scope =>
                {
                    using (scope)
                    {
                        Repository repository = scope.Repository;
                        repository.Create(new MongoWidget { Id = 1, Uuid = "a", Name = "Apple", Quantity = 5 });
                        repository.Create(new MongoWidget { Id = 2, Uuid = "b", Name = "Banana", Quantity = 5 });
                        repository.Create(new MongoWidget { Id = 3, Uuid = "c", Name = "Cherry", Quantity = 9 });

                        object definition = Builders<BsonDocument>.Filter.Gt(nameof(MongoWidget.Quantity), 5);
                        object document = new BsonDocument(nameof(MongoWidget.Quantity), 5);
                        object json = "{ Name: \"Banana\" }";
                        object filterObject = new NameFilter { Name = "Apple" };
                        object? nothing = null;

                        IEnumerable<MongoWidget> byDefinition = repository.Query<MongoWidget>(definition);
                        IEnumerable<MongoWidget> byDocument = repository.Query<MongoWidget>(document);
                        IEnumerable<MongoWidget> byJson = repository.Query<MongoWidget>(json);
                        IEnumerable<MongoWidget> byObject = repository.Query<MongoWidget>(filterObject);
                        IEnumerable<MongoWidget> all = repository.Query<MongoWidget>(nothing!);

                        dynamic? untypedNull = null;
                        bool untypedNullIsAmbiguous = false;
                        try
                        {
                            repository.Query<MongoWidget>(untypedNull);
                        }
                        catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
                        {
                            untypedNullIsAmbiguous = true;
                        }

                        return new BaseQueryOutcome(
                            byDefinition.Select(w => w.Name).ToArray(),
                            byDocument.Select(w => w.Name).OrderBy(n => n).ToArray(),
                            byJson.Select(w => w.Name).ToArray(),
                            byObject.Select(w => w.Name).ToArray(),
                            all.Count(),
                            untypedNullIsAmbiguous);
                    }
                })
                .TheTest
                .ShouldPass<BaseQueryOutcome>((because, outcome) =>
                {
                    because.ItsTrue("a filter definition is applied", outcome.ByDefinition.SequenceEqual(new[] { "Cherry" }));
                    because.ItsTrue("a BsonDocument is applied", outcome.ByDocument.SequenceEqual(new[] { "Apple", "Banana" }));
                    because.ItsTrue("a JSON string is applied", outcome.ByJson.SequenceEqual(new[] { "Banana" }));
                    because.ItsTrue("an object's properties are matched for equality", outcome.ByObject.SequenceEqual(new[] { "Apple" }));
                    because.ItsTrue("a null typed as object matches everything", outcome.AllCount == 3);
                    because.ItsTrue("an untyped dynamic null is ambiguous between the Query overloads", outcome.UntypedNullIsAmbiguous);
                })
                .SoBeHappy()
                .UnlessItFailed();
        }

        [IntegrationTest]
        public void RoundTripATypeWhoseKeyIsDeclaredOnABaseClass()
        {
            When.A<MongoTestScope>("creates, retrieves, updates and deletes a type whose Id and Uuid come from a base class",
                MongoTestScope.Open,
                scope =>
                {
                    using (scope)
                    {
                        bool updateFailedRaised = false;
                        scope.Repository.UpdateFailed += (_, _) => updateFailedRaised = true;
                        scope.Repository.Create(new MongoDerivedRecord { Id = 5, Uuid = "five", Name = "Original" });
                        scope.Repository.Create(new MongoDerivedRecord { Id = 6, Uuid = "six", Name = "Other" });
                        string? byId = scope.Repository.Retrieve<MongoDerivedRecord>(5UL)?.Name;
                        string? byUuid = scope.Repository.Retrieve<MongoDerivedRecord>("six")?.Name;
                        MongoDerivedRecord updated = scope.Repository.Update(new MongoDerivedRecord { Id = 5, Uuid = "five", Name = "Changed" });
                        string? afterUpdate = scope.Repository.Retrieve<MongoDerivedRecord>(5UL)?.Name;
                        long countAfterUpdate = scope.Count<MongoDerivedRecord>();
                        bool deleted = scope.Repository.Delete(new MongoDerivedRecord { Id = 5 });
                        return new BaseKeyOutcome(byId, byUuid, updated != null && !updateFailedRaised, afterUpdate, countAfterUpdate, deleted, scope.Repository.Retrieve<MongoDerivedRecord>(5UL) == null, scope.Count<MongoDerivedRecord>());
                    }
                })
                .TheTest
                .ShouldPass<BaseKeyOutcome>((because, outcome) =>
                {
                    because.ItsTrue("it is retrieved by the inherited id", outcome.ById == "Original");
                    because.ItsTrue("it is retrieved by the inherited uuid", outcome.ByUuid == "Other");
                    because.ItsTrue("updating a stored document succeeds", outcome.UpdateSucceeded);
                    because.ItsTrue("the update replaced the stored document", outcome.AfterUpdate == "Changed" && outcome.CountAfterUpdate == 2);
                    because.ItsTrue("the delete removed the document", outcome.Deleted && outcome.DeletedIsGone && outcome.CountAfterDelete == 1);
                })
                .SoBeHappy()
                .UnlessItFailed();
        }

        [IntegrationTest]
        public void RejectASecondDocumentWithTheSameNonIdKey()
        {
            When.A<MongoTestScope>("creates two instances of a [Key]-keyed type with the same key",
                MongoTestScope.Open,
                scope =>
                {
                    using (scope)
                    {
                        MongoKeyedThing first = scope.Repository.Create(new MongoKeyedThing { Number = 41, Name = "First" });
                        MongoKeyedThing second = scope.Repository.Create(new MongoKeyedThing { Number = 41, Name = "Second" });
                        bool writeErrorRecorded = scope.Repository.LastException is MongoWriteException;
                        MongoKeyedThing other = scope.Repository.Create(new MongoKeyedThing { Number = 42, Name = "Other" });
                        long countBeforeDelete = scope.Count<MongoKeyedThing>();
                        scope.Repository.Delete(new MongoKeyedThing { Number = 41 });
                        return new UniqueKeyOutcome(first != null, second == null, writeErrorRecorded, other != null, countBeforeDelete, scope.Repository.Retrieve<MongoKeyedThing>(41UL) == null, scope.Count<MongoKeyedThing>());
                    }
                })
                .TheTest
                .ShouldPass<UniqueKeyOutcome>((because, outcome) =>
                {
                    because.ItsTrue("the first create succeeds", outcome.FirstCreated);
                    because.ItsTrue("the second create with the same key returns null", outcome.SecondIsNull);
                    because.ItsTrue("LastException holds the duplicate key error", outcome.WriteErrorRecorded);
                    because.ItsTrue("a different key is still accepted", outcome.OtherCreated && outcome.CountBeforeDelete == 2);
                    because.ItsTrue("deleting by key leaves no document with that key", outcome.DeletedIsGone && outcome.CountAfterDelete == 1);
                })
                .SoBeHappy()
                .UnlessItFailed();
        }

        /// <summary>A repository over a database that exists for one test and is dropped on dispose.</summary>
        private sealed class MongoTestScope : IDisposable
        {
            private readonly MongoClient _client;
            private readonly string _databaseName;

            private MongoTestScope(MongoClient client, string databaseName)
            {
                _client = client;
                _databaseName = databaseName;
                Repository = new MongoRepository(ConnectionString, databaseName);
                Repository.AddType<MongoWidget>();
                Repository.AddType<MongoKeyedThing>();
                Repository.AddType<MongoDerivedRecord>();
            }

            public MongoRepository Repository { get; }

            public static MongoTestScope Open()
            {
                MongoClientSettings settings = MongoClientSettings.FromConnectionString(ConnectionString);
                settings.ServerSelectionTimeout = TimeSpan.FromSeconds(5);
                MongoClient client = new MongoClient(settings);
                client.GetDatabase("admin").RunCommand<BsonDocument>(new BsonDocument("ping", 1));
                return new MongoTestScope(client, "bamtest_" + Guid.NewGuid().ToString("N"));
            }

            public long Count<T>()
            {
                return _client.GetDatabase(_databaseName).GetCollection<BsonDocument>(typeof(T).FullName!).CountDocuments(Builders<BsonDocument>.Filter.Empty);
            }

            public void Dispose()
            {
                _client.DropDatabase(_databaseName);
            }
        }

        private sealed class NameFilter
        {
            public string Name { get; set; } = string.Empty;
        }

        private sealed record RetrieveOutcome(bool Created, string? NameByUnsignedLong, int? QuantityByLong, string? UuidByInt, bool MissingIsNull);
        private sealed record UuidOutcome(ulong? Id, string? Name, bool MissingIsNull);
        private sealed record UpdateOutcome(string? ChangedName, int? ChangedQuantity, string? UpsertedName, long DocumentCount);
        private sealed record DeleteOutcome(bool Deleted, bool DeletedAgain, bool DoomedIsGone, string? KeptName, long DocumentCount);
        private sealed record QueryOutcome(string[] ByDefinition, string[] ByDocument, string[] ByJson, string[] ByObject, int AllCount);
        private sealed record FailureOutcome(bool SecondIsNull, bool WriteExceptionRecorded, bool CreateFailedRaised, string? StoredName);
        private sealed record KeyedOutcome(string? Retrieved, string? Updated, long CountAfterUpdate, long CountAfterDelete);
        private sealed record BaseQueryOutcome(string[] ByDefinition, string[] ByDocument, string[] ByJson, string[] ByObject, int AllCount, bool UntypedNullIsAmbiguous);
        private sealed record BaseKeyOutcome(string? ById, string? ByUuid, bool UpdateSucceeded, string? AfterUpdate, long CountAfterUpdate, bool Deleted, bool DeletedIsGone, long CountAfterDelete);
        private sealed record UniqueKeyOutcome(bool FirstCreated, bool SecondIsNull, bool WriteErrorRecorded, bool OtherCreated, long CountBeforeDelete, bool DeletedIsGone, long CountAfterDelete);
    }

    /// <summary>A stored type whose key is the conventional <c>Id</c>, which MongoDB maps to <c>_id</c>.</summary>
    public class MongoWidget
    {
        public ulong Id { get; set; }
        public string Uuid { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Quantity { get; set; }
    }

    /// <summary>A stored type keyed by a <c>[Key]</c> property MongoDB does not treat as the document id.</summary>
    public class MongoKeyedThing
    {
        [Key]
        public ulong Number { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>The members <see cref="MongoDerivedRecord"/> inherits: its key and its uuid.</summary>
    public abstract class MongoBaseRecord
    {
        public ulong Id { get; set; }
        public string Uuid { get; set; } = string.Empty;
    }

    /// <summary>A stored type whose key is declared on its base class, as every RepoData type's is.</summary>
    public class MongoDerivedRecord : MongoBaseRecord
    {
        public string Name { get; set; } = string.Empty;
    }
}
