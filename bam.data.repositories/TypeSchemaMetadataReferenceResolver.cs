namespace Bam.Data.Repositories
{
    public class TypeSchemaMetadataReferenceResolver: TypeMetadataReferenceResolver
    {
        public TypeSchemaMetadataReferenceResolver(TypeSchema typeSchema): base(typeSchema.Tables.ToArray())
        {
        }
    }
}
