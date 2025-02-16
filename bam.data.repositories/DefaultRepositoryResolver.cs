using Bam.ServiceProxy;

namespace Bam.Data.Repositories
{
    public class DefaultRepositoryResolver : RepositoryResolver
    {
/*        public DefaultRepositoryResolver() : this(new DaoRepository())
        {
        }*/

        public DefaultRepositoryResolver(IRepository repository)
        {
            Repository = repository;
        }
        public IRepository Repository { get; set; }
        public override IRepository GetRepository(IHttpContext context)
        {
            return Repository;
        }
    }
}
