using Bam.CoreServices.AssemblyManagement;
using Microsoft.CodeAnalysis;

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
            // assemblies with no file (loaded from bytes) cannot be referenced by path and are skipped
            return AssemblyFileReferences.FromAssemblies(allTypes.Select(type => type.Assembly))
                .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
                .ToArray();
        }
    }
}
