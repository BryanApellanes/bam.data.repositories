﻿using Bam.Data.Repositories;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Bam.Net.Data.Repositories
{
    public class TemplatedWrapperGenerator : WrapperGenerator, ITemplatedWrapperGenerator
    {
        public TemplatedWrapperGenerator(ISchemaProvider schemaGenerator, ITemplateRenderer<WrapperModel> templateRenderer) : base(schemaGenerator)
        {
            this.TemplateRenderer = templateRenderer;
        }

        public ITemplateRenderer<WrapperModel> TemplateRenderer
        {
            get;
            private set;
        }

        readonly object _generateLock = new object();
        public override GeneratedAssemblyInfo GenerateAssembly()
        {
            lock (_generateLock)
            {
                string fileName = $"{WrapperNamespace}.Wrapper.dll";
                new DirectoryInfo(WriteSourceTo).ToAssembly(fileName, out CompilerResults results);
                GeneratedAssemblyInfo result = new GeneratedAssemblyInfo(fileName, results);
                result.Save();
                return result;
            }
        }

        public override void WriteSource(string writeSourceDir)
        {
            WriteSourceTo = writeSourceDir;
            foreach (Type type in TypeSchema.Tables)
            {
                WrapperModel model = new WrapperModel(type, TypeSchema, WrapperNamespace, DaoNamespace);
                string fileName = "{0}Wrapper.cs".Format(type.Name.TrimNonLetters());
                using (StreamWriter sw = new StreamWriter(Path.Combine(writeSourceDir, fileName)))
                {
                    sw.Write(model.Render());
                }
            }
        }
    }
}
