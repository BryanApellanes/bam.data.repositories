using Bam.Data.Schema;
using Newtonsoft.Json;
using System.Reflection;

namespace Bam.Data.Repositories
{
    public class GeneratedDaoAssemblyInfo: GeneratedAssemblyInfo
    {
        public GeneratedDaoAssemblyInfo() : base() { }
        public GeneratedDaoAssemblyInfo(string infoFileName, TypeSchema typeSchema, IDaoSchemaDefinition schemaDefintion) 
            : base(infoFileName)
        {
            TypeSchema = typeSchema;
            SchemaDefinition = schemaDefintion;
        }

        public GeneratedDaoAssemblyInfo(string infoFileName, Assembly assembly, byte[]? assemblyBytes = null) : base(
            infoFileName, assembly, assemblyBytes)
        {
        }

        [Exclude]
        [JsonIgnore]
        public ITypeSchema TypeSchema { get; set; } = null!;

        [Exclude]
        [JsonIgnore]
        public IDaoSchemaDefinition SchemaDefinition { get; set; } = null!;

        string _typeSchemaHash = null!;
        public string TypeSchemaHash
        {
            get
            {
                if (string.IsNullOrEmpty(_typeSchemaHash))
                {
                    _typeSchemaHash = TypeSchema?.Hash!;
                }

                return _typeSchemaHash!;
            }
            set
            {
                _typeSchemaHash = value;
            }
        }
        string _typeSchemaInfo = null!;
        public string TypeSchemaInfo
        {
            get
            {
                if (string.IsNullOrEmpty(_typeSchemaInfo))
                {
                    _typeSchemaInfo = TypeSchema?.ToString()!;
                }
                return _typeSchemaInfo!;
            }
            set
            {
                _typeSchemaInfo = value;
            }
        }

        string _schemaName = null!;
        public string SchemaName
        {
            get
            {
                if (string.IsNullOrEmpty(_schemaName) && SchemaDefinition != null)
                {
                    _schemaName = SchemaDefinition.Name;
                }
                return _schemaName;
            }
            set
            {
                _schemaName = value;
            }
        }
    }
}
