using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
