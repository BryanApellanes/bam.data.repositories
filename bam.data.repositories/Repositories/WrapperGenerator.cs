/*
	Copyright © Bryan Apellanes 2015  
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Reflection;
using System.CodeDom.Compiler;
using Bam.Data.Schema;

namespace Bam.Net.Data.Repositories
{
	/// <summary>
	/// A class used to generate Poco type wrappers which 
	/// enable lazy loading of IEnumerable properties.  This type
    /// is not thread safe
	/// </summary>
	public abstract class WrapperGenerator : IAssemblyGenerator, IWrapperGenerator
    {
        public WrapperGenerator(ISchemaProvider schemaGenerator)
        {
            DataNamespaces dataNamespaces = schemaGenerator.GetDataNamespaces();
            this.WrapperNamespace = dataNamespaces.WrapperNamespace;
            this.DaoNamespace = dataNamespaces.DaoNamespace;
            this.TypeSchema = schemaGenerator.CreateTypeSchema();
        }

		public string WrapperNamespace { get; set; }
        public string DaoNamespace { get; set; }
        public ITypeSchema TypeSchema { get; set; }
        public string WriteSourceTo { get; set; }
        public string InfoFileName => $"{WrapperNamespace}.Wrapper.genInfo.json";

        public virtual void Generate(ITypeSchema schema, string writeTo)
		{
            TypeSchema = schema;
            WriteSource(writeTo);
		}

        public Assembly GetGeneratedAssembly()
        {
            return GeneratedAssemblyInfo.GetGeneratedAssembly(InfoFileName, this).Assembly;
        }

        public abstract GeneratedAssemblyInfo GenerateAssembly();

        public abstract void WriteSource(string writeSourceDir);
    }
}
