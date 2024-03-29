/*
	Copyright © Bryan Apellanes 2015  
*/
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Bam.Console;
using Bam.Data.Repositories;
using Bam.Data.Schema;
using Bam.Net.Analytics;
using Bam.Net.Configuration;
using Bam.Net.CoreServices.AssemblyManagement;
using Bam.Net.Data.Qi;
using Bam.Net.Data.Schema;
using Bam.Net.Logging;
using Bam.Net.Services;
using Newtonsoft.Json;

namespace Bam.Net.Data.Repositories
{
    /// <summary>
    /// A class used to generate data access objects from
    /// CLR types.
    /// </summary>
    [Serializable]
    public class TypeToDaoGenerator : Loggable, IGeneratesDaoAssembly, IHasTypeSchemaTempPathProvider, ISourceGenerator
    {
        IDaoGenerator _daoGenerator;
        IWrapperGenerator _wrapperGenerator;
        ISchemaProvider _schemaProvider;
        HashSet<Assembly> _additionalReferenceAssemblies;
        HashSet<Type> _additionalReferenceTypes;

        public TypeToDaoGenerator(ISchemaProvider schemaProvider, IDaoGenerator daoGenerator, IWrapperGenerator? wrapperGenerator = null, ILogger? logger = null)
        {
            _baseNamespace = DataNamespaces.DefaultBaseNamespace;

            _schemaProvider = schemaProvider;

            _daoGenerator = daoGenerator;
            _daoGenerator.Namespace = this.DaoNamespace;
            SetWrapperGenerator(wrapperGenerator);

            _types = new HashSet<Type>();
            _additionalReferenceAssemblies = new HashSet<Assembly>();
            _additionalReferenceTypes = new HashSet<Type>();

            SetTempPathProvider();
            SubscribeToSchemaWarnings();
            if (logger != null)
            {
                Subscribe(logger);
            }
        }

        protected virtual void SetWrapperGenerator(IWrapperGenerator? wrapperGenerator)
        {
            if(wrapperGenerator != null)
            {
                _wrapperGenerator = wrapperGenerator;
                _wrapperGenerator.WrapperNamespace = this.WrapperNamespace;
                _wrapperGenerator.DaoNamespace = this.DaoNamespace;
            }
        }

        [Inject]
        public IDaoGenerator DaoGenerator
        {
            get => _daoGenerator;
            set => _daoGenerator = value;
        }

        [Inject]
        public IWrapperGenerator WrapperGenerator
        {
            get => _wrapperGenerator;
            set => _wrapperGenerator = value;
        }

        protected ISchemaProvider SchemaProvider
        {
            get => _schemaProvider;
            set => _schemaProvider = value;
        }

        /// <summary>
        /// A filter function used to exclude anonymous types
        /// that were created by the use of lambda functions from 
        /// having dao types generated
        /// </summary>
        public static Func<Type, bool> ClrDaoTypeFilter
        {
            get
            {
                return (t) => !t.IsAbstract && !t.HasCustomAttributeOfType<CompilerGeneratedAttribute>()
                && t.Attributes != (
                        TypeAttributes.NestedPrivate |
                        TypeAttributes.Sealed |
                        TypeAttributes.Serializable |
                        TypeAttributes.BeforeFieldInit
                    );
            }
        }

        public bool CheckIdField { get; set; }

        string _baseNamespace;

        /// <summary>
        /// The namespace containing POCO types to generate dao types for.  Setting 
        /// the BaseNamespace also sets the DaoNamespace
        /// and WrapperNamespace.
        /// </summary>
        public string BaseNamespace
        {
            get => _baseNamespace;
            set
            {
                _baseNamespace = value;
                _daoGenerator.Namespace = DaoNamespace;
                _wrapperGenerator.WrapperNamespace = WrapperNamespace;
                _wrapperGenerator.DaoNamespace = DaoNamespace;
            }
        }

        string _daoNamespace;
        public string DaoNamespace
        {
            get => _daoNamespace ?? $"{_baseNamespace}.Dao";
            set
            {
                _daoNamespace = value;
                _daoGenerator.Namespace = _daoNamespace;
                _wrapperGenerator.DaoNamespace = _daoNamespace;
            }
        }

        string _wrapperNamespace;
        public string WrapperNamespace
        {
            get => _wrapperNamespace ?? $"{_baseNamespace}.Wrappers";
            set
            {
                _wrapperNamespace = value;
                _wrapperGenerator.WrapperNamespace = _wrapperNamespace;                
            }
        }

        string _schemaName;
        public string SchemaName
        {
            get
            {
                if (string.IsNullOrEmpty(_schemaName))
                {
                    _schemaName = $"_{_types.ToInfoHash()}_Dao";
                }

                return _schemaName;
            }
            set => _schemaName = value;
        }

        public override void Subscribe(ILogger logger)
        {
            _schemaProvider.Subscribe(logger);
            base.Subscribe(logger);
        }

        HashSet<Type> _types;
        public Type[] Types => _types.ToArray();

        public bool KeepSource { get; set; }

        public void AddTypes(IEnumerable<Type> types)
        {
            foreach (Type type in types)
            {
                AddType(type);
            }
        }

        public void AddType(Type type)
        {
            if (!ClrDaoTypeFilter(type))
                return;

            if (type.GetProperty("Id") == null &&
                !type.HasCustomAttributeOfType<KeyAttribute>() &&
                CheckIdField)
            {
                throw new NoIdPropertyException(type);
            }

            if (type.HasEnumerableOfMe(type))
            {
                throw new NotSupportedException("Storable types cannot have enumerable properties that are of the same type as themselves.");
            }

            AddAdditionalReferenceAssemblies(new TypeInheritanceDescriptor(type));
            CustomAttributeTypeDescriptor attrs = new CustomAttributeTypeDescriptor(type);
            attrs.AttributeTypes.Each(attrType => AddAdditionalReferenceAssemblies(new TypeInheritanceDescriptor(attrType)));
            _types.Add(type);
        }

        public void AddReferenceAssembly(Assembly assembly)
        {
            _additionalReferenceAssemblies.Add(assembly);
        }

        private void AddAdditionalReferenceAssemblies(TypeInheritanceDescriptor typeInheritanceDescriptor)
        {
            if (!_additionalReferenceTypes.Contains(typeInheritanceDescriptor.Type))
            {
                AddAdditionalReferenceAssemblies(typeInheritanceDescriptor.Type);
                foreach (TypeTable type in typeInheritanceDescriptor.Chain)
                {
                    AddAdditionalReferenceAssemblies(type.Type);
                }
            }
        }
        private void AddAdditionalReferenceAssemblies(Type type)
        {
            if (!_additionalReferenceTypes.Contains(type))
            {
                _additionalReferenceAssemblies.Add(type.Assembly);
                foreach (MethodInfo method in type.GetMethods())
                {
                    _additionalReferenceAssemblies.Add(method.ReturnType.Assembly);
                    foreach (System.Reflection.ParameterInfo parameter in method.GetParameters())
                    {
                        _additionalReferenceAssemblies.Add(parameter.ParameterType.Assembly);
                    }
                }
                foreach (PropertyInfo property in type.GetProperties())
                {
                    _additionalReferenceAssemblies.Add(property.PropertyType.Assembly);
                }
                foreach (ConstructorInfo ctor in type.GetConstructors())
                {
                    foreach (System.Reflection.ParameterInfo parameter in ctor.GetParameters())
                    {
                        _additionalReferenceAssemblies.Add(parameter.ParameterType.Assembly);
                    }
                }
            }
        }

        public Assembly GetDaoAssembly(bool useExisting = true)
        {
            GeneratedDaoAssemblyInfo info = GeneratedAssemblies.GetGeneratedAssemblyInfo(SchemaName) as GeneratedDaoAssemblyInfo;
            if (info == null)
            {
                TypeSchema typeSchema = DaoSchemaDefinitionCreateResult.TypeSchema;
                IDaoSchemaDefinition schemaDef = DaoSchemaDefinitionCreateResult.DaoSchemaDefinition;
                string schemaName = schemaDef.Name;
                string schemaHash = typeSchema.Hash;
                info = new GeneratedDaoAssemblyInfo(schemaName, typeSchema, schemaDef);

                // check for the info file
                if (info.InfoFileExists && useExisting) // load it from file if it exists
                {
                    info = info.InfoFilePath.FromJsonFile<GeneratedDaoAssemblyInfo>();
                    if (info.TypeSchemaHash == null || !info.TypeSchemaHash.Equals(schemaHash)) // regenerate if the hashes don't match
                    {
                        ReportDiff(info, typeSchema);
                        GenerateOrThrow(schemaDef, typeSchema);
                    }
                    else
                    {
                        GeneratedAssemblies.SetAssemblyInfo(schemaName, info);
                    }
                }
                else
                {
                    GenerateOrThrow(schemaDef, typeSchema);
                }

                info = GeneratedAssemblies.GetGeneratedAssemblyInfo(SchemaName) as GeneratedDaoAssemblyInfo;
            }

            return info.GetAssembly();
        }

        DaoSchemaDefinitionCreateResult _schemaDefinitionCreateResult;
        object _schemaDefinitionCreateResultLock = new object();
        public DaoSchemaDefinitionCreateResult DaoSchemaDefinitionCreateResult
        {
            get
            {
                return _schemaDefinitionCreateResultLock.DoubleCheckLock(ref _schemaDefinitionCreateResult, () => CreateSchemaDefinition(SchemaName));
            }
        }

        [Verbosity(VerbosityLevel.Error, SenderMessageFormat = "Failed to generate DaoAssembly for {SchemaName}:\r\n {Message}")]
        public event EventHandler GenerateDaoAssemblyFailed;

        [Verbosity(VerbosityLevel.Information, SenderMessageFormat = "{Message}")]
        public event EventHandler GenerateDaoAssemblySucceeded;

        public string TempPath { get; set; }

        public string Message { get; set; }

        [Verbosity(VerbosityLevel.Warning, SenderMessageFormat = "Couldn't delete folder {TempPath}:\r\nMessage: {Message}")]
        public event EventHandler DeleteDaoTempFailed;

        public Func<IDaoSchemaDefinition, ITypeSchema, string> TypeSchemaTempPathProvider { get; set; }

        /// <summary>
        /// The event that is raised when a difference is detected in a previous run of the generator.
        /// </summary>
        [Verbosity(VerbosityLevel.Warning, SenderMessageFormat = "TypeSchema difference detected\r\n {OldInfoString} \r\n *** \r\n {NewInfoString}")]
        public event EventHandler SchemaDifferenceDetected;
        public string OldInfoString { get; set; }
        public string NewInfoString { get; set; }

        /// <summary>
        /// Warnings related to the type definitions, see TypeSchemaWarnings enum for possible warnings.
        /// </summary>
        public HashSet<ITypeSchemaWarning> TypeSchemaWarnings => DaoSchemaDefinitionCreateResult.TypeSchemaWarnings;
        public bool MissingColumns => DaoSchemaDefinitionCreateResult.MissingColumns;
        public SchemaWarnings Warnings => DaoSchemaDefinitionCreateResult.Warnings;

        public bool WarningsAsErrors
        {
            get; set;
        }

        [Verbosity(VerbosityLevel.Warning, EventArgsMessageFormat = "{Warning}: ParentType={ParentType}, ForeignKeyType={ForeignKeyType}")]
        public event EventHandler TypeSchemaWarning;

        [Verbosity(VerbosityLevel.Warning, SenderMessageFormat = "Missing {PropertyType} property: {ClassName}.{PropertyName}")]
        public event EventHandler SchemaWarning;
        
        protected internal void EmitWarnings()
        {
            if (MissingColumns)
            {
                if (this.Warnings.MissingForeignKeyColumns.Length > 0)
                {
                    foreach (ForeignKeyColumn fk in this.Warnings.MissingForeignKeyColumns)
                    {
                        DaoRepositorySchemaWarningEventArgs drswea = GetEventArgs(fk);
                        FireEvent(SchemaWarning, drswea);
                    }
                }
                if (this.Warnings.MissingKeyColumns.Length > 0)
                {
                    foreach (KeyColumn keyColumn in this.Warnings.MissingKeyColumns)
                    {
                        DaoRepositorySchemaWarningEventArgs drswea = GetEventArgs(keyColumn);
                        FireEvent(SchemaWarning, drswea);
                    }
                }
            }

            if (TypeSchemaWarnings.Count > 0)
            {
                foreach (TypeSchemaWarning warning in TypeSchemaWarnings)
                {
                    FireEvent(TypeSchemaWarning, warning.ToEventArgs());
                }
            }
        }

        public void ThrowWarningsIfWarningsAsErrors()
        {
            if (MissingColumns && WarningsAsErrors)
            {
                if (this.Warnings.MissingForeignKeyColumns.Length > 0)
                {
                    List<string> missingColumns = new List<string>();
                    foreach (ForeignKeyColumn fk in this.Warnings.MissingForeignKeyColumns)
                    {
                        DaoRepositorySchemaWarningEventArgs drswea = GetEventArgs(fk);
                        missingColumns.Add("{ClassName}.{PropertyName}".NamedFormat(drswea));
                    }
                    throw new MissingForeignKeyPropertyException(missingColumns);
                }
                if (this.Warnings.MissingKeyColumns.Length > 0)
                {
                    List<string> classNames = new List<string>();
                    foreach (KeyColumn k in this.Warnings.MissingKeyColumns)
                    {
                        DaoRepositorySchemaWarningEventArgs drswea = GetEventArgs(k);
                        classNames.Add(k.TableClassName);
                    }
                    throw new NoIdPropertyException(classNames);
                }
            }

            if (TypeSchemaWarnings.Count > 0 && WarningsAsErrors)
            {
                throw new TypeSchemaException(TypeSchemaWarnings.ToArray());
            }
        }

        /// <summary>
        /// Create a SchemaDefinitionCreateResult for the types currently
        /// added to the TypeDaoGenerator
        /// </summary>
        /// <param name="schemaName"></param>
        /// <returns></returns>
        protected internal DaoSchemaDefinitionCreateResult CreateSchemaDefinition(string schemaName = null)
        {
            return _schemaProvider.CreateDaoSchemaDefinition(_types, schemaName);
        }

        protected internal virtual bool GenerateDaoAssembly(ITypeSchema typeSchema, out Exception compilationEx)
        {
            try
            {
                compilationEx = null;
                IDaoSchemaDefinition schema = DaoSchemaDefinitionCreateResult.DaoSchemaDefinition;
                string assemblyName = $"{schema.Name}.dll";

                string writeSourceTo = TypeSchemaTempPathProvider(schema, typeSchema);
                byte[] binaryAssembly = GenerateAndCompile(assemblyName, writeSourceTo);
                Assembly assembly = Assembly.Load(binaryAssembly);
                GeneratedDaoAssemblyInfo info = new GeneratedDaoAssemblyInfo(schema.Name, assembly)
                {
                    TypeSchema = typeSchema,
                    SchemaDefinition = schema,
                    AssemblyBytes = binaryAssembly
                };
                info.Save();

                GeneratedAssemblies.SetAssemblyInfo(schema.Name, info);

                Message = "Type Dao Generation completed successfully";
                FireEvent(GenerateDaoAssemblySucceeded, new GenerateDaoAssemblyEventArgs(info));

                TryDeleteDaoTemp(writeSourceTo);

                return true;
            }
            catch(RoslynCompilationException rex)
            {
                Message = rex.GetMessageAndStackTrace();
                compilationEx = rex;
                FireGenerateDaoAssemblyFailed(rex);
                return false;
            }
            // TODO: eliminate the need for this catch block and delete it.
            catch (CompilationException cex)
            {
                Message = cex.GetMessageAndStackTrace();
                compilationEx = cex;
                FireGenerateDaoAssemblyFailed(cex);
                return false;
            }
        }

        protected void FireGenerateDaoAssemblySucceeded(GenerateDaoAssemblyEventArgs args)
        {
            FireEvent(GenerateDaoAssemblySucceeded, args);
        }
        
        protected void FireGenerateDaoAssemblyFailed(Exception ex = null)
        {
            FireEvent(GenerateDaoAssemblyFailed, new GenerateDaoAssemblyEventArgs(ex));
        }
        
        protected internal byte[] GenerateAndCompile(string assemblyNameToCreate, string writeSourceTo)
        {
            TryDeleteDaoTemp(writeSourceTo);
            GenerateSource(writeSourceTo);

            return Compile(assemblyNameToCreate, writeSourceTo);
        }

        /// <summary>
        /// Generate source code for the current set of types
        /// </summary>
        /// <param name="writeSourceTo"></param>
        public virtual void GenerateSource(string writeSourceTo)
        {
            EmitWarnings();
            ThrowWarningsIfWarningsAsErrors();
            GenerateDaos(DaoSchemaDefinitionCreateResult.DaoSchemaDefinition, writeSourceTo);
            GenerateWrappers(DaoSchemaDefinitionCreateResult.TypeSchema, writeSourceTo);
        }

        protected internal void GenerateDaos(IDaoSchemaDefinition schema, string writeSourceTo)
        {
            _daoGenerator.Generate(schema, writeSourceTo);
        }

        protected internal void GenerateWrappers(TypeSchema schema, string writeSourceTo)
        {
            _wrapperGenerator.Generate(schema, writeSourceTo);
        }

        protected internal byte[] Compile(string assemblyNameToCreate, string writeSourceTo)
        {
            RoslynCompiler compiler = new RoslynCompiler();
            compiler.AddMetadataReferenceResolver(new TypeSchemaMetadataReferenceResolver(DaoSchemaDefinitionCreateResult.TypeSchema));
            compiler.AddMetadataReferenceResolver(new DaoGeneratorMetadataReferenceResolver());
            compiler.AddMetadataReferenceResolver(new StaticAssemblyListReferencePackMetadataReferenceResolver("System.Xml.ReaderWriter"));
            return compiler.CompileDirectories(assemblyNameToCreate, new DirectoryInfo[] { new DirectoryInfo(writeSourceTo) });
        }

        protected HashSet<string> GetReferenceAssemblies()
        {
            HashSet<string> references = GetDefaultReferenceAssemblies();
            _additionalReferenceAssemblies.Each(asm =>
            {
                FileInfo assemblyInfo = asm.GetFileInfo();
                if (references.Contains(assemblyInfo.Name))
                {
                    references.Remove(assemblyInfo.Name); // removes System.Core.dll if it is later referenced by full path
                }

                references.Add(assemblyInfo.FullName);
            });
            DaoSchemaDefinitionCreateResult.TypeSchema.Tables.Each(type =>
            {
                references.Add(type.Assembly.GetFileInfo().FullName);
                CustomAttributeTypeDescriptor attrTypes = new CustomAttributeTypeDescriptor(type);
                attrTypes.AttributeTypes.Each(attrType => references.Add(attrType.Assembly.GetFileInfo().FullName));
            });
            references.Add(typeof(DaoRepository).Assembly.GetFileInfo().FullName);
            return references;
        }

        protected virtual HashSet<string> GetDefaultReferenceAssemblies()
        {
            HashSet<string> references = new HashSet<string>(RoslynCompiler.DefaultAssembliesToReference.Select(a => a.GetFileInfo().FullName).ToArray())
            {
                typeof(JsonIgnoreAttribute).Assembly.GetFileInfo().FullName,
                typeof(Qi.Qi).Assembly.GetFileInfo().FullName,
            };
            return references;
        }

        private static DaoRepositorySchemaWarningEventArgs GetEventArgs(KeyColumn keyColumn)
        {
            string className = keyColumn.TableClassName;
            DaoRepositorySchemaWarningEventArgs drswea = new DaoRepositorySchemaWarningEventArgs { ClassName = className, PropertyName = "Id", PropertyType = "key column" };
            return drswea;
        }
        private static DaoRepositorySchemaWarningEventArgs GetEventArgs(ForeignKeyColumn fk)
        {
            string referencingClassName = fk.ReferencingClass.EndsWith("Dao") ? fk.ReferencingClass.Truncate(3) : fk.ReferencingClass;
            string propertyName = fk.PropertyName;
            DaoRepositorySchemaWarningEventArgs drswea = new DaoRepositorySchemaWarningEventArgs { ClassName = referencingClassName, PropertyName = propertyName, PropertyType = "foreign key" };
            return drswea;
        }
        private void GenerateOrThrow(IDaoSchemaDefinition schema, TypeSchema typeSchema)
        {
            string tempPath = TypeSchemaTempPathProvider(schema, typeSchema);
            if (Directory.Exists(tempPath))
            {
                string newPath = tempPath.GetNextDirectoryName();
                Directory.Move(tempPath, newPath);
            }
            if (!GenerateDaoAssembly(typeSchema, out Exception compilationException))
            {
                throw new DaoGenerationException(SchemaName, typeSchema.Hash, Types.ToArray(), compilationException);
            }
        }

        protected internal bool TryDeleteDaoTemp(string writeSourceTo)
        {
            if (!KeepSource)
            {
                try
                {
                    TempPath = writeSourceTo;
                    if (Directory.Exists(writeSourceTo))
                    {
                        Directory.Delete(writeSourceTo, true);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    Message = ex.Message;
                    if (!string.IsNullOrEmpty(ex.StackTrace))
                    {
                        Message = $"{Message}\r\nStackTrace: {ex.StackTrace}";
                    }
                    FireEvent(DeleteDaoTempFailed, EventArgs.Empty);
                    return false;
                }
            }
            return false;
        }

        private void ReportDiff(GeneratedDaoAssemblyInfo info, TypeSchema typeSchema)
        {
            OldInfoString = info.TypeSchemaInfo ?? string.Empty;
            NewInfoString = typeSchema.ToString();
            DiffReport diff = DiffReport.Create(OldInfoString, NewInfoString);
            
            FireEvent(SchemaDifferenceDetected, new SchemaDifferenceEventArgs { GeneratedDaoAssemblyInfo = info, TypeSchema = typeSchema, DiffReport = diff });
        }

        private void SubscribeToSchemaWarnings()
        {
            _schemaProvider.Subscribe(VerbosityLevel.Warning, (l, a) =>
            {
                FireEvent(TypeSchemaWarning, l, a);
            });
        }
        
        // TODO: encapsulate this functionality and inject
        private void SetTempPathProvider()
        {
            TypeSchemaTempPathProvider = (schemaDef, typeSchema) =>
                System.IO.Path.Combine(RuntimeSettings.GenDir, "DaoTemp_{0}".Format(schemaDef.Name));
        }
    }
}
