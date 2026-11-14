using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace Bam.Data.Repositories
{
    /// <summary>
    /// Builds compiler references from assemblies. An assembly loaded from a file is referenced by its path.
    /// An assembly with no file (loaded from bytes, as runtime-compiled pocos and Dao assemblies are) is
    /// referenced from its in-memory metadata. Dynamic assemblies and assemblies in a collectible load context can't be referenced.
    /// </summary>
    public static class AssemblyFileReferences
    {
        /// <summary>
        /// A set for reference paths: case-insensitive on Windows, where paths that differ only in case are the same file.
        /// </summary>
        public static HashSet<string> NewPathSet()
        {
            return new HashSet<string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
        }

        /// <summary>
        /// Adds the full path of the file <paramref name="assembly"/> was loaded from to <paramref name="references"/>.
        /// Returns false, and adds nothing, for an assembly that has no file.
        /// </summary>
        public static bool TryAdd(ISet<string> references, Assembly assembly)
        {
            if (!assembly.TryGetFileInfo(out FileInfo? fileInfo))
            {
                return false;
            }

            references.Add(fileInfo.FullName);
            return true;
        }

        /// <summary>
        /// Gets the distinct file paths of the given assemblies, skipping assemblies that have no file.
        /// </summary>
        public static HashSet<string> FromAssemblies(IEnumerable<Assembly> assemblies)
        {
            HashSet<string> references = NewPathSet();
            foreach (Assembly assembly in assemblies)
            {
                TryAdd(references, assembly);
            }

            return references;
        }

        /// <summary>
        /// Creates a compiler reference from the metadata of an assembly that is already loaded, for an assembly
        /// that has no file. Returns false for a dynamic assembly, whose metadata isn't available, and for an
        /// assembly in a collectible load context.
        /// </summary>
        /// <remarks>
        /// The reference points at the loaded assembly's metadata in memory. That stays valid only while the
        /// assembly stays loaded, which is why collectible contexts are refused; an assembly loaded with
        /// <c>Assembly.Load(byte[])</c> is in a non-collectible context and is never unloaded.
        /// </remarks>
        public static unsafe bool TryCreateInMemoryReference(Assembly assembly, [NotNullWhen(true)] out MetadataReference? reference)
        {
            reference = null;
            if (assembly.IsDynamic || assembly.IsCollectible)
            {
                return false;
            }

            if (!System.Reflection.Metadata.AssemblyExtensions.TryGetRawMetadata(assembly, out byte* blob, out int length))
            {
                return false;
            }

            ModuleMetadata module = ModuleMetadata.CreateFromMetadata((IntPtr)blob, length);
            reference = AssemblyMetadata.Create(module).GetReference(display: assembly.FullName);
            return true;
        }
    }
}
