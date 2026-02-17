namespace Bam.Data.Repositories
{
    public class SchemaTypeModel
    {
        public Type Type { get; set; } = null!;
        public string DaoNamespace { get; set; } = null!;

        public static SchemaTypeModel FromType(Type type, string daoNamespace)
        {
            return new SchemaTypeModel { Type = type, DaoNamespace = daoNamespace };
        }
    }
}
