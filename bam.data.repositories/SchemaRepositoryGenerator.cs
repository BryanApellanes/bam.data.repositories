using System.Reflection;
using Bam.Data.Schema;
using Bam.Logging;

namespace Bam.Data.Repositories
{
    /// <summary>
    /// A code and assembly generator used to generate schema specific dao repositories.
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
            Logger = logger ?? Log.Default;

            DaoGenerator = new Schema.DaoGenerator(settings.DaoCodeWriter);
            WrapperGenerator = settings.WrapperGenerator;
            Configure(settings.DaoRepoGenerationConfig);
        }

        ILogger? _logger;
        protected ILogger? Logger
        {
            get => _logger;
            set
            {
                _logger = value;
                if(_logger != null)
                {
                    Subscribe(_logger);
                };
            } 
        }
        
        public ITemplateRenderer TemplateRenderer { get; protected set; } = null!;

        public IDaoRepoGenerationConfig Config
        {
            get; private set;
        } = null!;

        public override bool WarningsAsErrors
        {
            get => Config?.WarningsAsErrors ?? base.WarningsAsErrors;
            set => base.WarningsAsErrors = value;
        }
        
        public Assembly SourceAssembly { get; set; } = null!;

        public void Configure(IDaoRepoGenerationConfig? config)
        {
            if (config == null)
            {
                return;
            }
            Config = config;
            CheckIdField = config.CheckForIds;
            BaseRepositoryType = config.UseInheritanceSchema
                ? nameof(DaoInheritanceRepository)
                : config.UseAsync
                    ? nameof(AsyncDaoRepository)
                    : nameof(DaoRepository);
            BaseNamespace = Config.FromNamespace;            
        }

        public virtual SchemaTypeModel GetSchemaTypeModel(Type t)
        {
            return SchemaTypeModel.FromType(t, DaoNamespace);
        }

        public void AddTypes()
        {
            EnsureConfigOrDie();
            string assemblyPath = Config.TypeAssembly;
            if (assemblyPath.StartsWith("~"))
            {
                assemblyPath = new HomePath(assemblyPath);
            }
            SourceAssembly = Assembly.LoadFrom(assemblyPath);
            Args.ThrowIfNull(SourceAssembly, $"Assembly not found {assemblyPath}", "SourceAssembly");
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
            EnsureConfigOrDie();
            Args.ThrowIfNullOrEmpty(Config.WriteSourceTo, "WriteSourceTo");
            GenerateRepositorySource(Config.WriteSourceTo, Config.SchemaName);
        }

        public string BaseRepositoryType { get; set; } = null!;
        public string SchemaRepositoryNamespace => $"{DaoNamespace}.Repository";

        /// <summary>
        /// Generate all supporting source files for the schema specific DaoRepository including the repository itself.
        /// </summary>
        public void GenerateSource()
        {
            GenerateRepositorySource();
        }

        /// <summary>
        /// Generate all supporting source files for the schema specific DaoRepository including the repository itself.
        /// </summary>
        /// <param name="writeSourceTo"></param>
        public override void GenerateSource(string writeSourceTo)
        {
            GenerateRepositorySource(writeSourceTo);
        }

        /// <summary>
        /// Generate source code for the schema specific DaoRepository.
        /// </summary>
        /// <param name="writeSourceTo"></param>
        /// <param name="schemaName"></param>
        public virtual void GenerateRepositorySource(string writeSourceTo, string? schemaName = null)
        {
            AddTypes();
            Args.ThrowIf(Types.Length == 0, "No types were added");
            Args.ThrowIfNull(TemplateRenderer, "TemplateRenderer");

            string backupDirectory = writeSourceTo;
            while (Directory.Exists(backupDirectory))
            {
                backupDirectory = backupDirectory.GetNextDirectoryName();
            }

            if (!backupDirectory.Equals(writeSourceTo))
            {
                Directory.Move(writeSourceTo, backupDirectory);
            }
            
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
