namespace Bam.Data.Repositories
{
    public class TypeXrefModel: TypeXref
    {
        public TypeXrefModel() : base()
        { }

        public string DaoNamespace { get; set; } = null!;
        public static TypeXrefModel FromTypeXref(ITypeXref xref, string daoNamespace)
        {
            TypeXrefModel model = new TypeXrefModel();
            model.CopyProperties(xref);
            model.DaoNamespace = daoNamespace;
            return model;
        }
    }
}
