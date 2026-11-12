using System.Reflection;

namespace Bam.Data.Repositories
{
    /// <summary>
    /// Builds compiler reference paths from assemblies. An assembly with no file (loaded from bytes, as
    /// runtime-generated Dao assemblies are, or dynamic) cannot be referenced by path and is skipped.
    /// </summary>
    public static class AssemblyFileReferences
    {
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
            HashSet<string> references = new HashSet<string>();
            foreach (Assembly assembly in assemblies)
            {
                TryAdd(references, assembly);
            }

            return references;
        }
    }
}
