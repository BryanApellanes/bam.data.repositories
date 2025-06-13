namespace Bam.Data.Repositories;

public class DaoRepoData
{
    public DaoRepoData(object data, DaoRepository repository)
    {
        this.RepoData = data;
        this.Repository = repository;
    }
    
    public virtual object RepoData { get; set; }
    public DaoRepository Repository { get; set; }
}