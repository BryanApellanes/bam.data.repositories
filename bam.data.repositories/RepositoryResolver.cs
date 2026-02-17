using Bam.ServiceProxy;
using Bam.Logging;

namespace Bam.Data.Repositories
{
    public abstract class RepositoryResolver : Loggable, IRepositoryResolver
    {
        public ILogger Logger { get; set; } = null!;
        public DataSourceProvider DataSourceProvider { get; set; } = null!;
        public Func<IHttpContext, IRepository> GetRepositoryFunc { get; set; } = null!;

        public bool TryGetRepository<T>(IHttpContext context, out T repo) where T : IRepository
        {
            try
            {
                repo = GetRepository<T>(context);
                return true;
            }
            catch (Exception ex)
            {
                Log.Default!.AddEntry("Exception getting repository: {0}", ex, ex.Message);
                repo = default(T)!;
                return false;
            }
        }
        public T GetRepository<T>(IHttpContext context) where T : IRepository
        {
            return (T)GetRepository(context);
        }

        public abstract IRepository GetRepository(IHttpContext context);
    }
}
