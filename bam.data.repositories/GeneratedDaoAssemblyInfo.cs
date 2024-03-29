﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bam.Net.Data.Schema;
using Bam.Net;
using Newtonsoft.Json;
using System.CodeDom.Compiler;
using System.Reflection;

namespace Bam.Net.Data.Repositories
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

        public GeneratedDaoAssemblyInfo(string infoFileName, Assembly assembly, byte[] assemblyBytes = null) : base(
            infoFileName, assembly, assemblyBytes)
        {
        }

        [Exclude]
        [JsonIgnore]
        public ITypeSchema TypeSchema { get; set; }

        [Exclude]
        [JsonIgnore]
        public IDaoSchemaDefinition SchemaDefinition { get; set; }

        string _typeSchemaHash;
        public string TypeSchemaHash
        {
            get
            {
                if (string.IsNullOrEmpty(_typeSchemaHash))
                {
                    _typeSchemaHash = TypeSchema?.Hash;
                }

                return _typeSchemaHash;
            }
            set
            {
                _typeSchemaHash = value;
            }
        }
        string _typeSchemaInfo;
        public string TypeSchemaInfo
        {
            get
            {
                if (string.IsNullOrEmpty(_typeSchemaInfo))
                {
                    _typeSchemaInfo = TypeSchema?.ToString();
                }
                return _typeSchemaInfo;
            }
            set
            {
                _typeSchemaInfo = value;
            }
        }

        string _schemaName;
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
