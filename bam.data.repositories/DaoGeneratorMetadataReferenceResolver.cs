using Bam.CoreServices.AssemblyManagement;
using Bam.Net.Data.Qi;
using Microsoft.CodeAnalysis;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Bam.Data.Repositories
{
    public class DaoGeneratorMetadataReferenceResolver : IMetadataReferenceResolver
    {
        public MetadataReference[] GetMetaDataReferences()
        {
            return new HashSet<Assembly>
            {
                typeof(DynamicObject).Assembly,
                typeof(XmlDocument).Assembly,
                typeof(DataTable).Assembly,
                typeof(object).Assembly,
                typeof(JsonWriter).Assembly,
                typeof(Enumerable).Assembly,
                typeof(MarshalByValueComponent).Assembly,
                typeof(IComponent).Assembly,
                typeof(IServiceProvider).Assembly,
                typeof(Qi).Assembly,
                Assembly.GetExecutingAssembly()
            }
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .ToArray();
        }
    }
}
