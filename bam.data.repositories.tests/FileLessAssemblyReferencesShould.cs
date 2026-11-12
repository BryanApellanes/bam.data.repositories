using System.Reflection;
using Bam.DependencyInjection;
using Bam.Test;
using Microsoft.CodeAnalysis;

namespace Bam.Data.Repositories.Tests
{
    /// <summary>
    /// Compiler references are built from assembly files; an assembly loaded from bytes, as runtime-generated
    /// Dao assemblies are, has none and is skipped instead of throwing (bam.data.repositories#13).
    /// </summary>
    [UnitTestMenu("File-less assembly references should", Selector = "flar")]
    public class FileLessAssemblyReferencesShould : UnitTestMenuContainer
    {
        public FileLessAssemblyReferencesShould(ServiceRegistry serviceRegistry) : base(serviceRegistry)
        {
        }

        [UnitTest]
        public void SkipAnAssemblyLoadedFromBytes()
        {
            When.A<Assembly>("is loaded from bytes and listed with an assembly loaded from a file",
                LoadTestAssemblyFromBytes,
                bytesLoaded =>
                {
                    HashSet<string> references = AssemblyFileReferences.FromAssemblies(new Assembly[] { bytesLoaded, typeof(DaoRepository).Assembly });
                    bool addedBytesLoaded = AssemblyFileReferences.TryAdd(new HashSet<string>(), bytesLoaded);
                    return new ReferencePathsOutcome(references, addedBytesLoaded, typeof(DaoRepository).Assembly.Location);
                })
                .TheTest
                .ShouldPass<ReferencePathsOutcome>((because, _, outcome) =>
                {
                    because.ItsTrue("TryAdd reports that the byte-loaded assembly was not added", !outcome.AddedBytesLoaded);
                    because.ItsTrue("only the file-loaded assembly is referenced", outcome.References.Count == 1);
                    because.ItsTrue("the reference is that assembly's file", outcome.References.Contains(new FileInfo(outcome.FileLoadedPath).FullName));
                })
                .SoBeHappy()
                .UnlessItFailed();
        }

        [UnitTest]
        public void ResolveMetadataReferencesForATypeFromABytesLoadedAssembly()
        {
            When.A<Assembly>("supplies a type to the metadata reference resolver",
                LoadTestAssemblyFromBytes,
                bytesLoaded =>
                {
                    Type bytesLoadedType = bytesLoaded.GetType(typeof(FileLessAssemblyReferencesShould).FullName!, true)!;
                    TypeMetadataReferenceResolver resolver = new TypeMetadataReferenceResolver(bytesLoadedType, typeof(DaoRepository));
                    bool threw = false;
                    string[] paths = Array.Empty<string>();
                    try
                    {
                        paths = resolver.GetMetaDataReferences()
                            .Select(reference => ((PortableExecutableReference)reference).FilePath ?? string.Empty)
                            .ToArray();
                    }
                    catch (Exception)
                    {
                        threw = true;
                    }
                    return new MetadataReferencesOutcome(threw, paths, typeof(DaoRepository).Assembly.Location);
                })
                .TheTest
                .ShouldPass<MetadataReferencesOutcome>((because, _, outcome) =>
                {
                    because.ItsTrue("resolving does not throw", !outcome.Threw);
                    because.ItsTrue("the file-loaded type's assembly is referenced", outcome.Paths.Contains(new FileInfo(outcome.FileLoadedPath).FullName));
                    because.ItsTrue("every reference is an existing file", outcome.Paths.All(File.Exists));
                })
                .SoBeHappy()
                .UnlessItFailed();
        }

        private static Assembly LoadTestAssemblyFromBytes()
        {
            return Assembly.Load(File.ReadAllBytes(typeof(FileLessAssemblyReferencesShould).Assembly.Location));
        }

        private sealed record ReferencePathsOutcome(HashSet<string> References, bool AddedBytesLoaded, string FileLoadedPath);

        private sealed record MetadataReferencesOutcome(bool Threw, string[] Paths, string FileLoadedPath);
    }
}
