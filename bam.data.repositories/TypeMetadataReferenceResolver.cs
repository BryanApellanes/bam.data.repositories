using Bam.CoreServices.AssemblyManagement;
using Bam.Net;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bam.Data.Repositories
{
    public class TypeMetadataReferenceResolver : IMetadataReferenceResolver
    {
        public TypeMetadataReferenceResolver(params Type[] types) 
        {
            this.Types = new HashSet<Type>(types);
        }

        public HashSet<Type> Types { get; private set; }

        public MetadataReference[] GetMetaDataReferences()
        {
            HashSet<Type> allTypes = new HashSet<Type>();
            foreach(Type type in Types)
            {
                allTypes.Add(type);
                CustomAttributeTypeDescriptor customAttributeTypeDescriptor = new CustomAttributeTypeDescriptor(type);
                foreach(Type attrType in customAttributeTypeDescriptor.AttributeTypes)
                {
                    allTypes.Add(attrType);
                }
            }
            return new HashSet<MetadataReference>
            (
                allTypes.Select(type => MetadataReference.CreateFromFile(type.Assembly.GetFileInfo().FullName)).ToArray()
            ).ToArray();
        }
    }
}
