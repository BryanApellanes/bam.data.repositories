/*
	Copyright © Bryan Apellanes 2015
*/

using System.Reflection;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace Bam.Data.Repositories
{
    /// <summary>
    /// An incomplete <see cref="Repository"/> implementation backed by MongoDB. Each type is stored in a
    /// collection named after its full type name, and instances are matched by the key property
    /// <see cref="Repository.GetKeyProperty(Type)"/> resolves (a <c>[Key]</c> property, else <c>Id</c>).
    /// </summary>
    /// <remarks>
    /// MongoDB does not assign numeric keys, so the caller sets the key before <see cref="Create(object)"/>.
    /// A second <see cref="Create(object)"/> with a key that is already stored fails: the document id is
    /// unique by definition, and for a type whose key is not the document id the repository creates a unique
    /// index on the key element the first time it writes that type. Creating that index fails, and so does
    /// the write, when the collection already holds duplicate keys or a non-unique index on that element.
    /// The index is asked for once per type per repository instance: if the collection or the index is
    /// dropped while the repository lives, it is not created again until a new instance writes the type.
    /// <para>
    /// <see cref="ulong"/> members, keys included, are stored as Decimal128 so the whole range up to
    /// <see cref="ulong.MaxValue"/> fits (see <see cref="MongoUnsignedLongConvention"/>). The convention is
    /// registered process-wide the first time this class is used and applies to class maps created after
    /// that; documents that hold such a member as Int64 are still read and matched.
    /// </para>
    /// <para>
    /// A type with no usable key (no <c>[Key]</c> or <c>Id</c> property, or one that is not a
    /// <see cref="ulong"/>) can be created and queried, with the server-assigned or driver-mapped document id.
    /// Retrieve, Update and Delete match by key and fail for such a type.
    /// </para>
    /// <para>
    /// Create, Retrieve, Update and Delete do not throw: a failure is recorded in
    /// <see cref="Repository.LastException"/>, the matching failed event is raised, and the method returns
    /// null (or false). The Query overloads do throw what the driver throws. Query overloads that are not
    /// written yet throw <see cref="NotImplementedException"/>.
    /// </para>
    /// <para>
    /// A <see cref="BsonDocument"/>, JSON string or filter definition passed to Query is sent to the server
    /// as written, operators such as <c>$where</c> included. Never build one from untrusted input.
    /// </para>
    /// </remarks>
    public class MongoRepository : Repository, IQueryFilterable
    {
        private const string DocumentIdElementName = "_id";
        private const string UuidPropertyName = "Uuid";

        private readonly string _connectionString;
        private readonly string _databaseName;
        private readonly HashSet<Type> _keyIndexedTypes = new HashSet<Type>();
        private readonly object _keyIndexLock = new object();

        static MongoRepository()
        {
            // Before any class map exists, so every ulong member (keys included) can hold ulong.MaxValue.
            MongoUnsignedLongConvention.Register();
        }

        /// <summary>
        /// Creates a repository over the named database on the server the connection string points at.
        /// Nothing connects until the first operation.
        /// </summary>
        /// <param name="connectionString">A MongoDB connection string.</param>
        /// <param name="databaseName">The database that holds the collections.</param>
        public MongoRepository(string connectionString = "mongodb://localhost", string databaseName = "test") : base()
        {
            this._connectionString = connectionString;
            this._databaseName = databaseName;
        }

        IMongoClient _client = null!;
        readonly object _clientLock = new object();

        /// <summary>
        /// The client for the configured connection string, created on first use. Reading it does not
        /// open a connection.
        /// </summary>
        protected IMongoClient Client
        {
            get { return _clientLock.DoubleCheckLock(ref _client, () => new MongoClient(_connectionString)); }
        }

        IMongoDatabase _database = null!;
        readonly object _databaseLock = new object();

        /// <summary>
        /// The configured database, resolved on first use.
        /// </summary>
        protected IMongoDatabase Database
        {
            get { return _databaseLock.DoubleCheckLock(ref _database, () => GetDatabase(_databaseName)); }
        }

        /// <summary>
        /// Gets a database on the configured server by name.
        /// </summary>
        /// <param name="databaseName">The database name.</param>
        protected IMongoDatabase GetDatabase(string databaseName)
        {
            return Client.GetDatabase(databaseName);
        }

        /// <summary>
        /// Gets a collection of the configured database by name. Collections are read and written as
        /// <see cref="BsonDocument"/> because the stored type is only known at run time.
        /// </summary>
        /// <param name="collectionName">The collection name.</param>
        protected IMongoCollection<BsonDocument> GetCollection(string collectionName)
        {
            return Database.GetCollection<BsonDocument>(collectionName);
        }

        /// <summary>
        /// Gets the collection that stores <typeparamref name="T"/>, named after its full type name.
        /// </summary>
        /// <typeparam name="T">The stored type.</typeparam>
        protected IMongoCollection<BsonDocument> GetCollection<T>()
        {
            return GetCollection(typeof(T).FullName!);
        }

        /// <summary>
        /// Adds <typeparamref name="T"/> to the repository and registers its BSON class map. Class maps are
        /// process-wide, so a type another repository already registered is left as it is.
        /// </summary>
        /// <typeparam name="T">The type to store.</typeparam>
        public override void AddType<T>()
        {
            if (!BsonClassMap.IsClassMapRegistered(typeof(T)))
            {
                BsonClassMap.RegisterClassMap<T>();
            }

            base.AddType(typeof(T));
        }

        #region IRepository Members

        /// <inheritdoc />
        public override T Create<T>(T toCreate)
        {
            return (T)Create((object)toCreate);
        }

        /// <inheritdoc />
        public override object Create(object toCreate)
        {
            return Create(toCreate.GetType(), toCreate);
        }

        /// <summary>
        /// Inserts <paramref name="toCreate"/> into the collection for <paramref name="type"/>.
        /// </summary>
        /// <param name="type">The type whose collection receives the document.</param>
        /// <param name="toCreate">The instance to insert; its key must already be set.</param>
        /// <returns>The inserted instance, or null when the insert failed.</returns>
        public override object Create(Type type, object toCreate)
        {
            try
            {
                IMongoCollection<BsonDocument> collection = GetCollection(type.FullName!);
                EnsureKeyIsUnique(type, collection);
                collection.InsertOne(toCreate.ToBsonDocument(type));
                return toCreate;
            }
            catch (Exception ex)
            {
                LastException = ex;
                OnCreateFailed(new RepositoryEventArgs(toCreate));
                return null!;
            }
        }

        /// <inheritdoc />
        public override T Retrieve<T>(int id)
        {
            return Retrieve<T>((long)id);
        }

        /// <inheritdoc />
        public override T Retrieve<T>(long id)
        {
            return (T)Retrieve(typeof(T), id)!;
        }

        /// <inheritdoc />
        public override T Retrieve<T>(ulong id)
        {
            return (T)Retrieve(typeof(T), id)!;
        }

        /// <summary>
        /// Retrieves the instance of <paramref name="objectType"/> whose key equals <paramref name="id"/>.
        /// </summary>
        /// <returns>The instance, or null when there is none or the read failed.</returns>
        public override object Retrieve(Type objectType, long id)
        {
            try
            {
                return FindOne(objectType, KeyFilter(objectType, id))!;
            }
            catch (Exception ex)
            {
                LastException = ex;
                OnRetrieveFailed(new RepositoryEventArgs(id));
                return null!;
            }
        }

        /// <summary>
        /// Retrieves the instance of <paramref name="objectType"/> whose key equals <paramref name="id"/>.
        /// </summary>
        /// <returns>The instance, or null when there is none or the read failed.</returns>
        public override object Retrieve(Type objectType, ulong id)
        {
            try
            {
                return FindOne(objectType, KeyFilter(objectType, id))!;
            }
            catch (Exception ex)
            {
                LastException = ex;
                OnRetrieveFailed(new RepositoryEventArgs(id));
                return null!;
            }
        }

        /// <inheritdoc />
        public override T Retrieve<T>(string uuid)
        {
            return (T)Retrieve(typeof(T), uuid)!;
        }

        /// <summary>
        /// Retrieves the instance of <paramref name="objectType"/> whose <c>Uuid</c> property equals
        /// <paramref name="uuid"/>.
        /// </summary>
        /// <returns>The instance, or null when there is none or the read failed.</returns>
        public override object Retrieve(Type objectType, string uuid)
        {
            try
            {
                FilterDefinition<BsonDocument> filter = Builders<BsonDocument>.Filter.Eq(GetElementName(objectType, UuidPropertyName), uuid);
                return FindOne(objectType, filter)!;
            }
            catch (Exception ex)
            {
                LastException = ex;
                OnRetrieveFailed(new RepositoryEventArgs(uuid));
                return null!;
            }
        }

        public override IEnumerable<T> RetrieveAll<T>()
        {
            throw new NotImplementedException(); // see Mongo docs to implement this correctly
        }

        public override void BatchRetrieveAll(Type type, int batchSize, Action<IEnumerable<object>> processor)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<object> RetrieveAll(Type type)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<object> Query(Type type, Func<object, bool> predicate)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<T> Query<T>(Func<T, bool> predicate)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<T> Query<T>(Dictionary<string, object> queryParameters)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<object> Query(Type type, Dictionary<string, object> queryParameters)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Queries the collection for <typeparamref name="T"/>. <paramref name="query"/> may be a
        /// <see cref="FilterDefinition{TDocument}"/> of <see cref="BsonDocument"/>, a <see cref="BsonDocument"/>,
        /// a JSON filter string, or any object whose properties are matched for equality by element name.
        /// A null query matches every document; pass it typed as <see cref="object"/>, because an untyped null
        /// is ambiguous between the Query overloads.
        /// </summary>
        /// <remarks>
        /// The document, JSON and filter definition forms are raw filters: the server evaluates whatever
        /// operators they hold, <c>$where</c> included. Never build one from untrusted input. Driver
        /// exceptions are thrown to the caller.
        /// </remarks>
        /// <typeparam name="T">The stored type.</typeparam>
        /// <param name="query">The filter, in one of the forms above.</param>
        public override IEnumerable<T> Query<T>(dynamic query) //where T: new()
        {
            FilterDefinition<BsonDocument> filter = ToFilter((object?)query);
            return Query<T>(filter);
        }

        public override IEnumerable<object> Query(Type type, IQueryFilter query)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<T> Query<T>(IQueryFilter query)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override T Update<T>(T toUpdate)
        {
            return (T)Update((object)toUpdate!)!;
        }

        /// <inheritdoc />
        public override object Update(object toUpdate)
        {
            return Update(toUpdate.GetType(), toUpdate);
        }

        /// <summary>
        /// Replaces the stored document whose key matches <paramref name="toUpdate"/>, inserting it when
        /// there is none.
        /// </summary>
        /// <param name="type">The type whose collection holds the document.</param>
        /// <param name="toUpdate">The instance to save.</param>
        /// <returns>The saved instance, or null when the write failed.</returns>
        public override object Update(Type type, object toUpdate)
        {
            try
            {
                IMongoCollection<BsonDocument> collection = GetCollection(type.FullName!);
                EnsureKeyIsUnique(type, collection);
                BsonDocument document = toUpdate.ToBsonDocument(type);
                if (!HasIdMember(type) || GetStoredKeyElementName(type) == DocumentIdElementName)
                {
                    // A replacement may not change the stored _id. Leaving it out keeps the stored one, whether
                    // the server generated it or it is the key in an older representation (Int64 where this now
                    // writes Decimal128); on an insert the server takes it from the key filter. A type with an
                    // id member that is not its key keeps sending that member.
                    document.Remove(DocumentIdElementName);
                }

                collection.ReplaceOne(KeyFilter(type, toUpdate), document, new ReplaceOptions { IsUpsert = true });
                return toUpdate;
            }
            catch (Exception ex)
            {
                LastException = ex;
                OnUpdateFailed(new RepositoryEventArgs(toUpdate));
                return null!;
            }
        }

        /// <inheritdoc />
        public override bool Delete<T>(T toDelete)
        {
            return Delete((object)toDelete!);
        }

        /// <inheritdoc />
        public override bool Delete(object toDelete)
        {
            return Delete(toDelete.GetType(), toDelete);
        }

        /// <summary>
        /// Deletes the stored document whose key matches <paramref name="toDelete"/>.
        /// </summary>
        /// <param name="type">The type whose collection holds the document.</param>
        /// <param name="toDelete">The instance to delete.</param>
        /// <returns>True when the delete ran, whether or not a document matched; false when it failed.</returns>
        public override bool Delete(Type type, object toDelete)
        {
            try
            {
                IMongoCollection<BsonDocument> collection = GetCollection(type.FullName!);
                collection.DeleteOne(KeyFilter(type, toDelete));
                return true;
            }
            catch (Exception ex)
            {
                LastException = ex;
                OnDeleteFailed(new RepositoryEventArgs(toDelete));
                return false;
            }
        }

        public override IEnumerable<object> Query(string propertyName, object value)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<object> Query(dynamic query)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Queries the collection for <typeparamref name="T"/> with a driver filter and materializes the
        /// matches. A null filter matches every document. On a reference typed as
        /// <see cref="MongoRepository"/>, a dynamic argument that holds a filter definition, a
        /// <see cref="BsonDocument"/>, a JSON string or null binds here through the driver's implicit conversions.
        /// The filter is sent as written; never build one from untrusted input.
        /// </summary>
        /// <typeparam name="T">The stored type.</typeparam>
        /// <param name="filter">The filter to apply, or null for none.</param>
        public IEnumerable<T> Query<T>(FilterDefinition<BsonDocument>? filter)
        {
            filter ??= Builders<BsonDocument>.Filter.Empty;

            List<T> results = new List<T>();
            foreach (BsonDocument document in Find(typeof(T), filter).ToEnumerable())
            {
                results.Add((T)BsonSerializer.Deserialize(document, typeof(T)));
            }

            return results;
        }
        #endregion

        private object? FindOne(Type objectType, FilterDefinition<BsonDocument> filter)
        {
            BsonDocument? document = Find(objectType, filter).FirstOrDefault();
            return document == null ? null : BsonSerializer.Deserialize(document, objectType);
        }

        private IFindFluent<BsonDocument, BsonDocument> Find(Type objectType, FilterDefinition<BsonDocument> filter)
        {
            IFindFluent<BsonDocument, BsonDocument> find = GetCollection(objectType.FullName!).Find(filter);
            if (!HasIdMember(objectType))
            {
                // The type maps no member to _id, so the server-generated one would fail to deserialize.
                find = find.Project(Builders<BsonDocument>.Projection.Exclude(DocumentIdElementName));
            }

            return find;
        }

        private static FilterDefinition<BsonDocument> KeyFilter(Type type, object instance)
        {
            PropertyInfo keyProp = GetKeyProperty(type);
            return KeyFilter(type, keyProp, keyProp.GetValue(instance));
        }

        private static FilterDefinition<BsonDocument> KeyFilter(Type type, long id)
        {
            return KeyFilter(type, GetKeyProperty(type), id);
        }

        private static FilterDefinition<BsonDocument> KeyFilter(Type type, ulong id)
        {
            return KeyFilter(type, GetKeyProperty(type), id);
        }

        // Builds the filter from the value as the key member itself serializes it, so the filter matches what
        // Create stored whatever representation the class map gives the key.
        private static FilterDefinition<BsonDocument> KeyFilter(Type type, PropertyInfo keyProp, object? keyValue)
        {
            BsonMemberMap memberMap = GetMemberMap(type, keyProp.Name);
            Type keyType = Nullable.GetUnderlyingType(keyProp.PropertyType) ?? keyProp.PropertyType;
            object? typedValue = keyValue == null ? null : Convert.ChangeType(keyValue, keyType);
            BsonValue bsonValue = memberMap.GetSerializer().ToBsonValue(typedValue);
            return Builders<BsonDocument>.Filter.Eq(memberMap.ElementName, bsonValue);
        }

        private static string GetElementName(Type type, string memberName)
        {
            return GetMemberMap(type, memberName).ElementName;
        }

        // AllMemberMaps covers members declared on base classes; BsonClassMap.GetMemberMap sees only those
        // declared on the class itself. An unmapped member has no known element name, so nothing is guessed.
        private static BsonMemberMap GetMemberMap(Type type, string memberName)
        {
            BsonMemberMap? memberMap = BsonClassMap.LookupClassMap(type).AllMemberMaps.FirstOrDefault(map => map.MemberName == memberName);
            if (memberMap == null)
            {
                throw new InvalidOperationException($"{type.FullName} has no BSON member map for {memberName}; it cannot be matched against stored documents.");
            }

            return memberMap;
        }

        // A key that is not the document id is unique only if an index says so. Asked of the server once per
        // type for the life of this repository; the server treats a repeat as a no-op. The lock guards the set
        // only: the round trip runs outside it, so a slow or failing index build for one type does not hold up
        // writes of the others.
        private void EnsureKeyIsUnique(Type type, IMongoCollection<BsonDocument> collection)
        {
            lock (_keyIndexLock)
            {
                if (_keyIndexedTypes.Contains(type))
                {
                    return;
                }
            }

            string? keyElementName = GetStoredKeyElementName(type);
            if (keyElementName != null && keyElementName != DocumentIdElementName)
            {
                CreateIndexModel<BsonDocument> uniqueKey = new CreateIndexModel<BsonDocument>(
                    Builders<BsonDocument>.IndexKeys.Ascending(keyElementName),
                    new CreateIndexOptions { Unique = true });
                collection.Indexes.CreateOne(uniqueKey);
            }

            lock (_keyIndexLock)
            {
                _keyIndexedTypes.Add(type);
            }
        }

        // The element the key is stored under, or null when the type has no key the repository can match on
        // (no [Key] or Id property, a key that is not a ulong, or a key the class map does not store). Such a
        // type can still be created and queried; there is nothing to keep unique.
        private static string? GetStoredKeyElementName(Type type)
        {
            PropertyInfo keyProp;
            try
            {
                keyProp = GetKeyProperty(type);
            }
            catch (NoIdPropertyException)
            {
                return null;
            }
            catch (InvalidIdPropertyTypeException)
            {
                return null;
            }

            if (keyProp == null)
            {
                return null;
            }

            return BsonClassMap.LookupClassMap(type).AllMemberMaps.FirstOrDefault(map => map.MemberName == keyProp.Name)?.ElementName;
        }

        private static bool HasIdMember(Type type)
        {
            return BsonClassMap.LookupClassMap(type).IdMemberMap != null;
        }

        private static FilterDefinition<BsonDocument> ToFilter(object? query)
        {
            switch (query)
            {
                case null:
                    return Builders<BsonDocument>.Filter.Empty;
                case FilterDefinition<BsonDocument> filter:
                    return filter;
                case BsonDocument document:
                    return new BsonDocumentFilterDefinition<BsonDocument>(document);
                case string json:
                    return new BsonDocumentFilterDefinition<BsonDocument>(BsonDocument.Parse(json));
                default:
                    return new BsonDocumentFilterDefinition<BsonDocument>(query.ToBsonDocument(query.GetType()));
            }
        }
    }
}
