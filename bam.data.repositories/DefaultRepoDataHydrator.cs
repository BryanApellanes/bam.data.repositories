using Bam.Logging;

namespace Bam.Data.Repositories
{
    public class DefaultRepoDataHydrator : RepoDataHydrator
    {
        public override void Hydrate(IRepoData data, IRepository repository)
        {
            Log.Debug("Hydrate called on ({0}) for repo ({1}).", data?.ToString()!, repository?.ToString()!);
            Hydrator?.Invoke(data!, repository!);
        }

        public Action<IRepoData, IRepository> Hydrator { get; set; } = null!;
    }
}
