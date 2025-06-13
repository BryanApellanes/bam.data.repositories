namespace Bam.Data.Repositories;

public class DaoRepoData<T> : DaoRepoData
{
    public DaoRepoData(T data, DaoRepository repository) 
        : base(data, repository)
    {
        this.RepoData = data;
    }

    public static implicit operator T(DaoRepoData<T> result)
    {
        ulong id = result.RepoData.Property<ulong>("Id");
        return (T)result.Repository.Retrieve(typeof(T), id);
    }
    
    public new T RepoData { get; set; }
}