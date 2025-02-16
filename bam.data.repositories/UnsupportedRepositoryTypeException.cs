namespace Bam.Data.Repositories
{
    public class UnsupportedRepositoryTypeException: Exception
    {
        public UnsupportedRepositoryTypeException(Type unsupportedRepoType) : base($"Query method taking parameter of type QueryFilter must delegate to DaoRepositories, designated SourceRepository is a {unsupportedRepoType.Name}")
        { }
    }
}
