using Bam.Net.Data.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bam.Data.Repositories
{
    public class TypeSchemaMetadataReferenceResolver: TypeMetadataReferenceResolver
    {
        public TypeSchemaMetadataReferenceResolver(TypeSchema typeSchema): base(typeSchema.Tables.ToArray())
        {
        }
    }
}
