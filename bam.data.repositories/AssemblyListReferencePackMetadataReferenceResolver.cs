using Bam.CoreServices.AssemblyManagement;
using Bam.Net;
using Microsoft.CodeAnalysis;
using MongoDB.Bson.Serialization.IdGenerators;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
