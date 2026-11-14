using System.Reflection;
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

        /// <summary>
        /// The assemblies the last call to <see cref="GetMetaDataReferences"/> could not reference: dynamic assemblies
        /// and those in a collectible load context. Each is named by <c>GetFilePathOrName</c>, so a failed compile
        /// can be traced to the reference it is missing.
        /// </summary>
        public string[] SkippedAssemblies { get; private set; } = Array.Empty<string>();

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

            // an assembly loaded from a file is referenced by path; one loaded from bytes (a runtime-compiled poco)
            // has no file and is referenced from its in-memory metadata
            HashSet<string> paths = AssemblyFileReferences.NewPathSet();
            List<MetadataReference> inMemory = new List<MetadataReference>();
            List<string> skipped = new List<string>();
            foreach (Assembly assembly in allTypes.Select(type => type.Assembly).Distinct())
            {
                if (AssemblyFileReferences.TryAdd(paths, assembly))
                {
                    continue;
                }

                if (AssemblyFileReferences.TryCreateInMemoryReference(assembly, out MetadataReference? reference))
                {
                    inMemory.Add(reference);
                    continue;
                }

                skipped.Add(assembly.GetFilePathOrName());
            }

            SkippedAssemblies = skipped.ToArray();
            return paths
                .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))
                .Concat(inMemory)
                .ToArray();
        }
    }
}
