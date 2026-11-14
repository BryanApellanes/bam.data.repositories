using System.Reflection;
using Bam.DependencyInjection;
using Bam.Test;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Bam.Data.Repositories.Tests
{
    /// <summary>
    /// An assembly loaded from bytes, as runtime-compiled pocos and Dao assemblies are, has no file. Compiler
    /// references for it come from its in-memory metadata, and log text names it instead of throwing
    /// (bam.data.repositories#13).
    /// </summary>
    [UnitTestMenu("File-less assembly references should", Selector = "flar")]
    public class FileLessAssemblyReferencesShould : UnitTestMenuContainer
    {
        public FileLessAssemblyReferencesShould(ServiceRegistry serviceRegistry) : base(serviceRegistry)
        {
        }

        [UnitTest]
        public void LeaveAnAssemblyLoadedFromBytesOutOfThePathSet()
        {
            When.A<Assembly>("is loaded from bytes and listed with an assembly loaded from a file",
                LoadTestAssemblyFromBytes,
                bytesLoaded =>
                {
                    HashSet<string> references = AssemblyFileReferences.FromAssemblies(new Assembly[] { bytesLoaded, typeof(DaoRepository).Assembly });
                    bool addedBytesLoaded = AssemblyFileReferences.TryAdd(AssemblyFileReferences.NewPathSet(), bytesLoaded);
                    return new ReferencePathsOutcome(references, addedBytesLoaded, typeof(DaoRepository).Assembly.Location);
                })
                .TheTest
                .ShouldPass<ReferencePathsOutcome>((because, _, outcome) =>
                {
                    because.ItsTrue("TryAdd reports that the byte-loaded assembly was not added", !outcome.AddedBytesLoaded);
                    because.ItsTrue("only the file-loaded assembly has a path", outcome.References.Count == 1);
                    because.ItsTrue("the path is that assembly's file", outcome.References.Contains(new FileInfo(outcome.FileLoadedPath).FullName));
                })
                .SoBeHappy()
                .UnlessItFailed();
        }

        [UnitTest]
        public void ReferenceATypeFromABytesLoadedAssemblyFromMemory()
        {
            When.A<Assembly>("supplies a type to the metadata reference resolver",
                LoadTestAssemblyFromBytes,
                bytesLoaded =>
                {
                    Type bytesLoadedType = bytesLoaded.GetType(typeof(FileLessProbePoco).FullName!, true)!;
                    TypeMetadataReferenceResolver resolver = new TypeMetadataReferenceResolver(bytesLoadedType, typeof(DaoRepository));
                    MetadataReference[] references = resolver.GetMetaDataReferences();
                    string[] filePaths = references.OfType<PortableExecutableReference>().Select(reference => reference.FilePath).OfType<string>().ToArray();
                    int inMemoryCount = references.Count(reference => reference is not PortableExecutableReference fileReference || fileReference.FilePath == null);
                    return new MetadataReferencesOutcome(filePaths, inMemoryCount, resolver.SkippedAssemblies, typeof(DaoRepository).Assembly.Location);
                })
                .TheTest
                .ShouldPass<MetadataReferencesOutcome>((because, _, outcome) =>
                {
                    because.ItsTrue("nothing is skipped", outcome.Skipped.Length == 0);
                    because.ItsTrue("the byte-loaded assembly gets one in-memory reference", outcome.InMemoryCount == 1);
                    because.ItsTrue("the file-loaded type's assembly is referenced by its file", outcome.FilePaths.Contains(new FileInfo(outcome.FileLoadedPath).FullName));
                    because.ItsTrue("every file reference is an existing file", outcome.FilePaths.All(File.Exists));
                })
                .SoBeHappy()
                .UnlessItFailed();
        }

        [UnitTest]
        public void CompileAgainstAPocoFromABytesLoadedAssembly()
        {
            When.A<Assembly>("holds a poco and was loaded from bytes, as a runtime-compiled poco is",
                LoadTestAssemblyFromBytes,
                bytesLoaded =>
                {
                    Type bytesLoadedPoco = bytesLoaded.GetType(typeof(FileLessProbePoco).FullName!, true)!;
                    TypeMetadataReferenceResolver resolver = new TypeMetadataReferenceResolver(bytesLoadedPoco, typeof(object));
                    List<MetadataReference> references = new List<MetadataReference>(resolver.GetMetaDataReferences());
                    string runtimeDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
                    references.Add(MetadataReference.CreateFromFile(Path.Combine(runtimeDirectory, "System.Runtime.dll")));
                    string source = $"public class ProbeDao {{ public {typeof(FileLessProbePoco).FullName} Poco {{ get; set; }} = new {typeof(FileLessProbePoco).FullName}(); public string Name => Poco.Name; }}";
                    CSharpCompilation compilation = CSharpCompilation.Create(
                        $"probe_{Guid.NewGuid():N}",
                        new[] { CSharpSyntaxTree.ParseText(source) },
                        references,
                        new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
                    using MemoryStream output = new MemoryStream();
                    Microsoft.CodeAnalysis.Emit.EmitResult result = compilation.Emit(output);
                    string[] errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Select(d => d.ToString()).ToArray();
                    return new CompileOutcome(result.Success, errors);
                })
                .TheTest
                .ShouldPass<CompileOutcome>((because, _, outcome) =>
                {
                    because.ItsTrue($"code using the byte-loaded poco compiles ({string.Join("; ", outcome.Errors)})", outcome.Success);
                })
                .SoBeHappy()
                .UnlessItFailed();
        }

        [UnitTest]
        public void DescribeAMissingDaoTypeInAnAssemblyLoadedFromBytes()
        {
            When.A<Assembly>("is a dao assembly loaded from bytes with no dao for the poco",
                LoadTestAssemblyFromBytes,
                bytesLoaded =>
                {
                    bool threw = false;
                    string message = string.Empty;
                    try
                    {
                        message = DaoRepository.DescribeMissingDaoType(typeof(FileLessProbePoco), bytesLoaded, typeof(FileLessProbePoco), "Probe.Dao");
                    }
                    catch (Exception)
                    {
                        threw = true;
                    }
                    return new MissingDaoOutcome(threw, message, bytesLoaded.FullName!);
                })
                .TheTest
                .ShouldPass<MissingDaoOutcome>((because, _, outcome) =>
                {
                    because.ItsTrue("describing it does not throw", !outcome.Threw);
                    because.ItsTrue("the dao assembly is named by its full name", outcome.Message.Contains($"Dao Assembly={outcome.AssemblyFullName}"));
                    because.ItsTrue("the poco is named by its full type name", outcome.Message.Contains($"pocoType ({typeof(FileLessProbePoco).FullName})"));
                })
                .SoBeHappy()
                .UnlessItFailed();
        }

        private static Assembly LoadTestAssemblyFromBytes()
        {
            return Assembly.Load(File.ReadAllBytes(typeof(FileLessAssemblyReferencesShould).Assembly.Location));
        }

        private sealed record ReferencePathsOutcome(HashSet<string> References, bool AddedBytesLoaded, string FileLoadedPath);

        private sealed record MetadataReferencesOutcome(string[] FilePaths, int InMemoryCount, string[] Skipped, string FileLoadedPath);

        private sealed record CompileOutcome(bool Success, string[] Errors);

        private sealed record MissingDaoOutcome(bool Threw, string Message, string AssemblyFullName);
    }

    /// <summary>
    /// A poco that depends only on the runtime, so code compiled against it needs no other reference.
    /// </summary>
    public class FileLessProbePoco
    {
        public string Name { get; set; } = string.Empty;
    }
}
