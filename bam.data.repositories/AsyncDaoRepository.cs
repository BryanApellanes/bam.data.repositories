using Bam.Data.Schema;
using Bam.Logging;

namespace Bam.Data.Repositories
{
    /// <summary>
    /// A DaoRepository that also implements IAsyncRepository and IAsyncSchemaRepository,
    /// providing async versions of all repository and schema operations.
    /// </summary>
    public class AsyncDaoRepository : DaoRepository, IAsyncRepository, IAsyncSchemaRepository
    {
        public AsyncDaoRepository() : base()
        {
        }

        public AsyncDaoRepository(ISchemaProvider schemaProvider, IDaoGenerator daoGenerator, IWrapperGenerator wrapperGenerator)
            : base(schemaProvider, daoGenerator, wrapperGenerator)
        {
        }

        public AsyncDaoRepository(ISchemaProvider schemaProvider, IDaoGenerator daoGenerator, IWrapperGenerator wrapperGenerator, IDatabase? database, ILogger? logger)
            : base(schemaProvider, daoGenerator, wrapperGenerator, database, logger)
        {
        }

        #region IAsyncRepository Members

        public Task<T> CreateAsync<T>(T instance) where T : class, new()
        {
            return Task.Run(() => Create(instance));
        }

        public Task<bool> DeleteAsync(object toDelete)
        {
            return Task.Run(() => Delete(toDelete));
        }

        public Task<bool> DeleteAsync<T>(T toDelete) where T : new()
        {
            return Task.Run(() => Delete(toDelete));
        }

        public Task<IEnumerable<object>> QueryAsync(dynamic query)
        {
            return Task.Run((Func<IEnumerable<object>>)(() => Query(query)));
        }

        public Task<IEnumerable<object>> QueryAsync(Type type, Dictionary<string, object> queryParams)
        {
            return Task.Run(() => Query(type, queryParams));
        }

        public Task<IEnumerable<object>> QueryAsync(Type type, Func<object, bool> predicate)
        {
            return Task.Run(() => Query(type, predicate));
        }

        public Task<IEnumerable<object>> QueryAsync(string propertyName, object propertyValue)
        {
            return Task.Run(() => Query(propertyName, propertyValue));
        }

        public Task<IEnumerable<T>> QueryAsync<T>(Func<T, bool> query) where T : class, new()
        {
            return Task.Run(() => Query(query));
        }

        public Task<IEnumerable<T>> QueryAsync<T>(Dictionary<string, object> queryParams) where T : class, new()
        {
            return Task.Run(() => Query<T>(queryParams));
        }

        public Task<IEnumerable<object>> RetrieveAllAsync(Type type)
        {
            return Task.Run(() => RetrieveAll(type));
        }

        public Task<IEnumerable<T>> RetrieveAllAsync<T>() where T : class, new()
        {
            return Task.Run(() => RetrieveAll<T>());
        }

        public Task<object> RetrieveAsync(Type objectType, string uuid)
        {
            return Task.Run(() => Retrieve(objectType, uuid));
        }

        public Task<object> RetrieveAsync(Type objectType, long id)
        {
            return Task.Run(() => Retrieve(objectType, id));
        }

        public Task<T> RetrieveAsync<T>(long id) where T : class, new()
        {
            return Task.Run(() => Retrieve<T>(id));
        }

        public Task<T> RetrieveAsync<T>(int id) where T : class, new()
        {
            return Task.Run(() => Retrieve<T>(id));
        }

        public Task<object> SaveAsync(object instance)
        {
            return Task.Run(() => Save(instance))!;
        }

        public new Task<T> SaveAsync<T>(T instance) where T : class, new()
        {
            return Task.Run(() => Save<T>(instance));
        }

        public Task<object> UpdateAsync(object toUpdate)
        {
            return Task.Run(() => Update(toUpdate));
        }

        public Task<T> UpdateAsync<T>(T toUpdate) where T : new()
        {
            return Task.Run(() => Update<T>(toUpdate));
        }

        #endregion

        #region IAsyncSchemaRepository Members

        public Task SetOneWhereAsync<T>(IQueryFilter where) where T : new()
        {
            return Task.Run(() => SetOneWhere<T>(where));
        }

        public Task<T?> GetOneWhereAsync<T>(IQueryFilter where) where T : new()
        {
            return Task.Run(() => GetOneWhere<T>(where));
        }

        public Task<T?> OneWhereAsync<T>(IQueryFilter where) where T : new()
        {
            return Task.Run(() => OneWhere<T>(where));
        }

        public Task<IEnumerable<T>> WhereAsync<T>(IQueryFilter where) where T : new()
        {
            return Task.Run(() => Where<T>(where));
        }

        public Task<IEnumerable<T>> TopWhereAsync<T>(int count, IQueryFilter where) where T : new()
        {
            return Task.Run(() => TopWhere<T>(count, where));
        }

        public Task<long> CountAsync<T>() where T : new()
        {
            return Task.Run(() => Count<T>());
        }

        public Task<long> CountWhereAsync<T>(IQueryFilter where) where T : new()
        {
            return Task.Run(() => CountWhere<T>(where));
        }

        #endregion
    }
}
