using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Bam.Data;
using Bam.Data.Schema;
using Bam.Net.Logging;

namespace Bam.Net.Data.Repositories
{
    /// <summary>
    /// A code and assembly generator used to generate schema
    /// specific dao repositories
    /// </summary>
    public class SchemaRepositoryGenerator : TypeToDaoGenerator, IRepositorySourceGenerator
    {
        public SchemaRepositoryGenerator(ISchemaRepositoryGeneratorSettings settings, ILogger? logger = null)
            :base(
                 new SchemaProvider(), 
                 new Schema.DaoGenerator(settings.DaoCodeWriter), 
                 settings.WrapperGenerator
            )
        {
            if (logger != null)
            {
                Subscribe(logger);
            }

            DaoGenerator = new Schema.DaoGenerator(settings.DaoCodeWriter);
            WrapperGenerator = settings.WrapperGenerator;
            Configure(settings.DaoRepoGenerationConfig);
        }

        public ITemplateRenderer TemplateRenderer { get; protected set; }

        public IDaoRepoGenerationConfig Config
        {
            get; private set;
        }

        public Assembly SourceAssembly { get; set; }

        public void Configure(IDaoRepoGenerationConfig config)
        {
            if (config == null)
            {
                return;
            }
            Config = config;
            CheckIdField = config.CheckForIds;
            BaseRepositoryType = config.UseInheritanceSchema ? "DaoInheritanceRepository" : "DaoRepository";
            BaseNamespace = Config.FromNamespace;
            
        }

        public virtual SchemaTypeModel GetSchemaTypeModel(Type t)
        {
            return SchemaTypeModel.FromType(t, DaoNamespace);
        }

        public void AddTypes()
        {
            EnsureConfigOrDie();
            SourceAssembly = Assembly.LoadFile(Config.TypeAssembly);
            Args.ThrowIfNull(SourceAssembly, $"Assembly not found {Config.TypeAssembly}", "SourceAssembly");
            AddTypes(SourceAssembly, Config.FromNamespace);
        }

        public void AddTypes(Assembly typeAssembly, string baseNamespace)
        {
            BaseNamespace = baseNamespace;
            Args.ThrowIfNull(typeAssembly);
            AddTypes(typeAssembly.GetTypes().Where(t => t.Namespace != null && t.Namespace.Equals(baseNamespace)));
        }

        public void GenerateRepositorySource()
        {
            AddTypes();
            Args.ThrowIf(Types.Length == 0, "No types were added");
            Args.ThrowIfNullOrEmpty(Config.WriteSourceTo, "WriteSourceTo");
            GenerateRepositorySource(Config.WriteSourceTo, Config.SchemaName);
        }

        public string BaseRepositoryType { get; set; }
        public string SchemaRepositoryNamespace => $"{DaoNamespace}.Repository";

        /// <summary>
        /// Generate all supporting source files for the schema specific DaoRepository including the repository itself.
        /// </summary>
        public void GenerateSource()
        {
            EnsureConfigOrDie();
            GenerateSource(Config.WriteSourceTo);
        }

        /// <summary>
        /// Generate all supporting source files for the schema specific DaoRepository including the repository itself.
        /// </summary>
        /// <param name="writeSourceTo"></param>
        public override void GenerateSource(string writeSourceTo)
        {
            base.GenerateSource(writeSourceTo);
            GenerateRepositorySource(writeSourceTo);
        }

        /// <summary>
        /// Generate source code for the schema specific DaoRepository.
        /// </summary>
        /// <param name="writeSourceTo"></param>
        /// <param name="schemaName"></param>
        public virtual void GenerateRepositorySource(string writeSourceTo, string schemaName = null)
        {
            Args.ThrowIfNull(TemplateRenderer, "TemplateRenderer");

            schemaName = schemaName ?? SchemaName;
            SchemaName = schemaName;
            base.GenerateSource(writeSourceTo);
            SchemaRepositoryModel schemaModel = new SchemaRepositoryModel
            {
                BaseRepositoryType = BaseRepositoryType,
                BaseNamespace = BaseNamespace,
                SchemaRepositoryNamespace = SchemaRepositoryNamespace,
                SchemaName = schemaName,
                Types = Types.Select(GetSchemaTypeModel).ToArray()
            };

            string filePath = Path.Combine(writeSourceTo, $"{schemaName}Repository.cs");
            if (File.Exists(filePath))
            {
                File.Move(filePath, filePath.GetNextFileName());
            }
            TemplateRenderer.Render("SchemaRepository", schemaModel, new FileStream(filePath, FileMode.Create));
        }

        private void EnsureConfigOrDie()
        {
            if (Config == null)
            {
                Args.Throw<InvalidOperationException>("{0} not configured, first call Configure(GenerationConfig)", GetType().Name);
            }
        }
    }
}
