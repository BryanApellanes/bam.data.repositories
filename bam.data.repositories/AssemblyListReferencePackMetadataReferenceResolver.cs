using Bam.CoreServices.AssemblyManagement;
using Microsoft.CodeAnalysis;

namespace Bam.Data.Repositories
{
    public abstract class AssemblyListReferencePackMetadataReferenceResolver : IMetadataReferenceResolver
    {
        public AssemblyListReferencePackMetadataReferenceResolver() 
        {
        }

        public abstract string[] GetAssemblyFileNames();

        public MetadataReference[] GetMetaDataReferences()
        {
            List<MetadataReference> references = new List<MetadataReference>();
            foreach(string assemblyFileName in GetAssemblyFileNames())
            {
                references.Add(MetadataReference.CreateFromFile(RuntimeSettings.ResolveReferenceAssemblyPathOrDie(assemblyFileName)));
            }

            return references.ToArray();
        }
    }
}
