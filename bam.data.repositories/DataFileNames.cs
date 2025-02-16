using Bam.Data.Schema;

namespace Bam.Data.Repositories
{
    public class DataFileNames
    {
        public DataFileNames(DataNamespaces dataNamespaces)
        {
            this.DataNamespaces = dataNamespaces;
        }

        public DataNamespaces DataNamespaces { get; private set; }

        public string GetWrapperAssemblyName()
        {
            return $"{this.DataNamespaces.WrapperNamespace}.dll";
        }

        public string GetWrapperCodeFileName(Type type, string fileExtension = ".cs")
        {
            return $"{type.Name.TrimNonLetters()}Wrapper{fileExtension}";
        }
    }
}
