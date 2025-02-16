namespace Bam.Data.Repositories
{
    public class StaticAssemblyListReferencePackMetadataReferenceResolver : AssemblyListReferencePackMetadataReferenceResolver
    {
        public StaticAssemblyListReferencePackMetadataReferenceResolver(params string[] assemblyNamesToReference)
        {
            this.AssemblyFileNames = assemblyNamesToReference;
        }

        public string[] AssemblyFileNames { get; private set; }

        public override string[] GetAssemblyFileNames()
        {
            return this.AssemblyFileNames;
        }
    }
}
