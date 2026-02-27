namespace Bam.Data.Repositories
{
    /// <summary>
    /// Async versions of the generic schema repository methods.
    /// </summary>
    public interface IAsyncSchemaRepository : ISchemaRepository, IAsyncRepository
    {
        Task SetOneWhereAsync<T>(IQueryFilter where) where T : new();
        Task<T?> GetOneWhereAsync<T>(IQueryFilter where) where T : new();
        Task<T?> OneWhereAsync<T>(IQueryFilter where) where T : new();
        Task<IEnumerable<T>> WhereAsync<T>(IQueryFilter where) where T : new();
        Task<IEnumerable<T>> TopWhereAsync<T>(int count, IQueryFilter where) where T : new();
        Task<long> CountAsync<T>() where T : new();
        Task<long> CountWhereAsync<T>(IQueryFilter where) where T : new();
    }
}
