namespace Bam.Data.Repositories
{
    public partial class SchemaRepositoryModel
    {
        public SchemaRepositoryModel()
        {
            BaseRepositoryType = "DaoRepository";
        }
        public string SchemaName { get; set; } = null!;
        public SchemaTypeModel[] Types { get; set; } = null!;
        public string BaseNamespace { get; set; } = null!;
        public string SchemaRepositoryNamespace { get; set; } = null!;
        public string BaseRepositoryType { get; set; }

    }
}
