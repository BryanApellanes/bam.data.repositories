namespace Bam.Data.Repositories
{
    public class NamedAuditRepoData : KeyedAuditRepoData
    {
        [CompositeKey]
        public string Name { get; set; } = null!;
    }
}