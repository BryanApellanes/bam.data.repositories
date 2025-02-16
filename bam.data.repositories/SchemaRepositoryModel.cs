namespace Bam.Data.Repositories
{
    public partial class SchemaRepositoryModel
    {
        public SchemaRepositoryModel()
        {
            BaseRepositoryType = "DaoRepository";
        }
        public string SchemaName { get; set; }
        public SchemaTypeModel[] Types { get; set; }
        public string BaseNamespace { get; set; }
        public string SchemaRepositoryNamespace { get; set; }
        public string BaseRepositoryType { get; set; }

    }
}
