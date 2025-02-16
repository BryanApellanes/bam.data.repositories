namespace Bam.Data.Repositories
{
    // TODO: rename this to NamedRepoData
    public class FsRepoData: AuditRepoData
    {
        public FsRepoData() : base()
        {
            Name = Cuid;
        }
        public FsRepoData(string name)
        {
            Name = name;
        }
        
        public string Name { get; set; }
    }
}
